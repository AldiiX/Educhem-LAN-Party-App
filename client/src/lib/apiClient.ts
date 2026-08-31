const csrfCookieNames = ["__Host-edlp_csrf", "edlp_csrf"] as const;
const unsafeMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

let csrfPromise: Promise<string> | null = null;
let refreshPromise: Promise<boolean> | null = null;

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

export async function refreshAccessToken() {
    refreshPromise ??= (async () => {
        const response = await fetch("/api/v1/auth/refresh", {
            method: "POST",
            credentials: "include",
            cache: "no-store",
        });
        if(!response.ok) return false;
        await getCsrfToken(true);
        return true;
    })().catch(() => false).finally(() => {
        refreshPromise = null;
    });

    return refreshPromise;
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
    if(response.ok && url.includes("/api/v1/auth/login")) await getCsrfToken(true);

    return response;
}
