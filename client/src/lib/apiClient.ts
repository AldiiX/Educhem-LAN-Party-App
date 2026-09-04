const csrfCookieNames = ["__Host-edlp_csrf", "edlp_csrf"] as const;
const accessExpiresCookieNames = ["__Host-edlp_access_expires", "edlp_access_expires"] as const;
const unsafeMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

let csrfPromise: Promise<string> | null = null;
type RefreshResult = "refreshed" | "unauthorized" | "unavailable";
let refreshPromise: Promise<RefreshResult> | null = null;

export class AuthenticationRequiredError extends Error {
    constructor() {
        super("Přihlášení vypršelo. Přihlaste se znovu.");
        this.name = "AuthenticationRequiredError";
    }
}

function readCookie(names: readonly string[]) {
    if(typeof document === "undefined") return null;

    for(const part of document.cookie.split(";")) {
        const separator = part.indexOf("=");
        if(separator < 0) continue;

        const name = part.slice(0, separator).trim();
        if(!names.includes(name)) continue;
        return decodeURIComponent(part.slice(separator + 1));
    }

    return null;
}

async function getCsrfToken(force = false) {
    const current = readCookie(csrfCookieNames);
    if(current && !force) return current;

    csrfPromise ??= fetch("/api/v1/auth/csrf", {
        method: "GET",
        credentials: "include",
        cache: "no-store",
    }).then(response => {
        if(!response.ok) throw new Error("Nepodarilo se ziskat CSRF token.");
        const token = readCookie(csrfCookieNames);
        if(!token) throw new Error("Backend nevratil CSRF token.");
        return token;
    }).finally(() => {
        csrfPromise = null;
    });

    return csrfPromise;
}

function isAuthEndpoint(input: RequestInfo | URL) {
    const url = typeof input === "string" ? input : input instanceof URL ? input.pathname : input.url;
    return url.includes("/api/v1/auth/")
        || url.includes("/api/v1/account/forgot-password")
        || url.includes("/api/v1/account/reset-password");
}

async function createRequestInit(input: RequestInfo | URL, init: RequestInit = {}) {
    const method = (init.method ?? "GET").toUpperCase();
    const headers = new Headers(init.headers);

    if(unsafeMethods.has(method) && !isAuthEndpoint(input)) {
        headers.set("X-XSRF-TOKEN", await getCsrfToken());
    }

    return {
        ...init,
        method,
        credentials: "include" as const,
        headers,
    };
}

function canRefresh(input: RequestInfo | URL) {
    const url = typeof input === "string" ? input : input instanceof URL ? input.pathname : input.url;
    return !url.includes("/api/v1/auth/login")
        && !url.includes("/api/v1/auth/refresh")
        && !url.includes("/api/v1/auth/csrf");
}

async function refreshAccessTokenResult(): Promise<RefreshResult> {
    refreshPromise ??= (async () => {
        const response = await fetch("/api/v1/auth/refresh", {
            method: "POST",
            credentials: "include",
            cache: "no-store",
        });
        if(response.status === 401) return "unauthorized" as const;
        if(!response.ok) return "unavailable" as const;
        await getCsrfToken(true);
        return "refreshed" as const;
    })().catch(() => "unavailable" as const).finally(() => {
        refreshPromise = null;
    });

    return refreshPromise;
}

export async function refreshAccessToken() {
    return await refreshAccessTokenResult() === "refreshed";
}

/**
 * obnovi access token jen pri chybejici nebo blizky expiraci; cookie je jen hint pro klienta
 * vraci true, kdyz probehl refresh; neplatna session a vypadek serveru maji odlisny chyby
 */
export async function ensureFreshAccessToken(force = false): Promise<boolean> {
    const expiresAt = Number(readCookie(accessExpiresCookieNames)) * 1000;
    if(!force && Number.isFinite(expiresAt) && expiresAt > Date.now() + 15_000) return false;

    const result = await refreshAccessTokenResult();
    if(result === "unauthorized") throw new AuthenticationRequiredError();
    if(result !== "refreshed") throw new Error("Přihlášení teď nelze obnovit. Zkoušíme připojení znovu.");
    return true;
}

export async function apiFetch(input: RequestInfo | URL, init: RequestInit = {}) {
    let response = await fetch(input, await createRequestInit(input, init));

    if(response.headers.get("X-CSRF-Invalid") === "1") {
        await getCsrfToken(true);
        response = await fetch(input, await createRequestInit(input, init));
    }

    if(response.status === 401 && canRefresh(input) && await refreshAccessToken()) {
        response = await fetch(input, await createRequestInit(input, init));
    }

    const url = typeof input === "string" ? input : input instanceof URL ? input.pathname : input.url;
    if(response.ok && (url.includes("/api/v1/auth/login") || url.endsWith("/api/v1/account/login-link"))) {
        await getCsrfToken(true);
    }

    return response;
}
