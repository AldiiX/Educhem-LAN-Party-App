"use client"

import {
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    HttpTransportType,
    IHttpConnectionOptions,
    LogLevel,
    NullLogger
} from "@microsoft/signalr";
import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {AuthenticatedHubHttpClient} from "@/lib/AuthenticatedHubHttpClient";
import {AuthenticationRequiredError} from "@/lib/apiClient";

type HubStatus =
    | "idle"
    | "connecting"
    | "connected"
    | "reconnecting"
    | "disconnected"
    | "authentication-required"
    | "error";

type HubEventHandler = (...args: any[]) => void;

type HubEventHandlers = Record<string, HubEventHandler>;

type UseSignalRHubOptions = {
    enabled?: boolean;
    requireAuthentication?: boolean;
    baseUrl?: string;
    handlers?: HubEventHandlers;
    logLevel?: LogLevel;
    reconnectDelays?: number[];
    startRetryDelays?: number[];
    accessTokenFactory?: () => string | Promise<string>;
    withCredentials?: boolean;
    transport?: HttpTransportType;
    skipNegotiation?: boolean;
    serverTimeout?: number;
    keepAliveInterval?: number;
};

function wait(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function isAbsoluteUrl(url: string) {
    return /^https?:\/\//i.test(url);
}

function createHubUrl(url: string, baseUrl?: string) {
    if(isAbsoluteUrl(url) || !baseUrl) {
        return url;
    }

    const normalizedBaseUrl = baseUrl.replace(/\/+$/, "");
    const normalizedUrl = url.startsWith("/") ? url : `/${url}`;

    return `${normalizedBaseUrl}${normalizedUrl}`;
}

function toError(error: unknown) {
    if(error instanceof Error) {
        return error;
    }

    return new Error(String(error));
}

function useLatestRef<T>(value: T) {
    const ref = useRef(value);

    useEffect(() => {
        ref.current = value;
    }, [value]);

    return ref;
}

export function useSignalRHub(url: string, options: UseSignalRHubOptions = {}) {
    const {
        enabled = true,
        requireAuthentication = false,
        baseUrl,
        handlers = {},
        logLevel = LogLevel.Warning,
        reconnectDelays = [0, 2000, 10000],
        startRetryDelays = [0, 1000, 3000],
        accessTokenFactory,
        withCredentials,
        transport,
        skipNegotiation,
        serverTimeout = 30000,
        keepAliveInterval = 10000
    } = options;

    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [status, setStatus] = useState<HubStatus>("idle");
    const [lastError, setLastError] = useState<Error | null>(null);

    const handlersRef = useLatestRef(handlers);
    const accessTokenFactoryRef = useLatestRef(accessTokenFactory);

    const fullUrl = useMemo(() => {
        return createHubUrl(url, baseUrl);
    }, [url, baseUrl]);

    const handlersKey = useMemo(() => {
        return Object.keys(handlers).sort().join("|");
    }, [handlers]);

    const reconnectDelaysKey = useMemo(() => {
        return reconnectDelays.join("|");
    }, [reconnectDelays]);

    const startRetryDelaysKey = useMemo(() => {
        return startRetryDelays.join("|");
    }, [startRetryDelays]);

    useEffect(() => {
        if(!enabled) {
            setStatus("idle");
            setConnection(null);
            return;
        }

        let disposed = false;
        let authenticationError: AuthenticationRequiredError | null = null;

        const connectionOptions: IHttpConnectionOptions = {};
        let connectionUrl = fullUrl;

        if(requireAuthentication) {
            const authenticatedUrl = new URL(fullUrl, window.location.origin);
            authenticatedUrl.searchParams.set("requireAuthentication", "true");
            connectionUrl = authenticatedUrl.toString();
            connectionOptions.httpClient = new AuthenticatedHubHttpClient(NullLogger.instance, error => {
                authenticationError = error;
                if(disposed) return;
                setStatus("authentication-required");
                setLastError(error);
            });
        }

        if(accessTokenFactoryRef.current) {
            connectionOptions.accessTokenFactory = () => {
                return accessTokenFactoryRef.current?.() ?? "";
            };
        }

        if(typeof withCredentials === "boolean") {
            connectionOptions.withCredentials = withCredentials;
        }

        if(typeof transport !== "undefined") {
            connectionOptions.transport = transport;
        }

        if(typeof skipNegotiation === "boolean") {
            connectionOptions.skipNegotiation = skipNegotiation;
        }
        // kontrola expirace bezi pred negotiate i pri reconnectu, ten se nesmi preskocit
        if(requireAuthentication) connectionOptions.skipNegotiation = false;

        const builder = new HubConnectionBuilder()
            .withUrl(connectionUrl, connectionOptions)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: context => authenticationError || disposed
                    ? null
                    : reconnectDelays[context.previousRetryCount] ?? null
            })
            .configureLogging(logLevel);

        if(typeof serverTimeout === "number") {
            builder.withServerTimeout(serverTimeout);
        }

        if(typeof keepAliveInterval === "number") {
            builder.withKeepAliveInterval(keepAliveInterval);
        }

        const newConnection = builder.build();

        const registeredHandlers: Array<{
            eventName: string;
            handler: HubEventHandler;
        }> = [];

        for(const eventName of Object.keys(handlersRef.current)) {
            const handler = (...args: any[]) => {
                handlersRef.current[eventName]?.(...args);
            };

            newConnection.on(eventName, handler);

            registeredHandlers.push({
                eventName,
                handler
            });
        }

        newConnection.onreconnecting(error => {
            if(disposed) return;

            setStatus("reconnecting");
            setLastError(error ? toError(error) : null);
        });

        newConnection.onreconnected(() => {
            if(disposed) return;

            setStatus("connected");
            setLastError(null);
        });

        newConnection.onclose(error => {
            if(disposed) return;

            if(authenticationError) {
                setStatus("authentication-required");
                setLastError(authenticationError);
                return;
            }
            setStatus(error ? "error" : "disconnected");
            setLastError(error ? toError(error) : null);
        });

        async function startConnection() {
            for(const delay of startRetryDelays) {
                if(disposed) return;

                if(delay > 0) {
                    await wait(delay);
                }
                if(disposed || authenticationError) return;

                try {
                    setStatus("connecting");

                    await newConnection.start();

                    if(disposed) {
                        await newConnection.stop().catch(error => {
                            console.error("signalr stop failed", error);
                        });
                        return;
                    }

                    setConnection(newConnection);
                    setStatus("connected");
                    setLastError(null);
                    return;
                } catch(error) {
                    if(disposed || authenticationError) return;

                    setLastError(toError(error));
                }
            }

            if(disposed) return;

            setStatus("error");
        }

        const startTimeout = setTimeout(() => {
            void startConnection();
        }, 0);

        return () => {
            disposed = true;
            clearTimeout(startTimeout);

            for(const registeredHandler of registeredHandlers) {
                newConnection.off(registeredHandler.eventName, registeredHandler.handler);
            }

            if(
                newConnection.state !== HubConnectionState.Disconnected
            ) {
                newConnection.stop().catch(error => {
                    console.error("signalr stop failed", error);
                });
            }

            setConnection(null);
        };
    }, [
        enabled,
        requireAuthentication,
        fullUrl,
        logLevel,
        handlersKey,
        reconnectDelaysKey,
        startRetryDelaysKey,
        withCredentials,
        transport,
        skipNegotiation,
        serverTimeout,
        keepAliveInterval
    ]);

    const invoke = useCallback(async <T = unknown>(methodName: string, ...args: unknown[]) => {
        if(!connection || connection.state !== HubConnectionState.Connected) {
            const error = new Error("signalr connection is not connected");
            setLastError(error);
            throw error;
        }

        try {
            return await connection.invoke<T>(methodName, ...args);
        } catch(error) {
            setLastError(toError(error));
            throw error;
        }
    }, [connection]);

    const send = useCallback(async (methodName: string, ...args: unknown[]) => {
        if(!connection || connection.state !== HubConnectionState.Connected) {
            const error = new Error("signalr connection is not connected");
            setLastError(error);
            throw error;
        }

        try {
            await connection.send(methodName, ...args);
        } catch(error) {
            setLastError(toError(error));
            throw error;
        }
    }, [connection]);

    const stop = useCallback(async () => {
        if(!connection) return;

        await connection.stop();
        setStatus("disconnected");
    }, [connection]);

    return {
        connection,
        status,
        lastError,
        invoke,
        send,
        stop,
        isConnected: status === "connected",
        isConnecting: status === "connecting",
        isReconnecting: status === "reconnecting",
        isDisconnected: status === "disconnected",
        isAuthenticationRequired: status === "authentication-required",
        hasError: status === "error"
    };
}
