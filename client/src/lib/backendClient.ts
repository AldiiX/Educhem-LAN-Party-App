import {headers} from "next/headers";
import {BACKEND_URL} from "@/lib/vars";

export class BackendUnavailableError extends Error {
    public readonly statusCode: number;

    constructor(message = "Backend je nedostupný") {
        super(message);
        this.name = "BackendUnavailableError";
        this.statusCode = 403;
    }
}

function requireBackendBaseUrl() {
    if (! BACKEND_URL) {
        throw new Error("BACKEND_BASE_URL is missing");
    }
    return BACKEND_URL;
}

export async function fetchBackendJson<T>(path: string, init?: RequestInit): Promise<T | null> {
    const baseUrl = requireBackendBaseUrl();

    const h = await headers();
    const cookieHeader = h.get("cookie") ?? "";

    let response: Response;

    try {
        response = await fetch(baseUrl + path, {
            cache: "force-cache",
            next: {revalidate: 5},
            ...init,
            headers: {
                ...(init?.headers ?? {}),
                cookie: cookieHeader,
            },
        });
    } catch {
        throw new BackendUnavailableError("Backend is unavailable");
    }

    if (! response.ok) {
        throw new Error(`Backend returned ${response.status}`);
    }

    const text = await response.text();
    if (! text) {
        return null;
    }

    try {
        return JSON.parse(text) as T;
    } catch {
        throw new Error("Invalid backend JSON response");
    }
}
