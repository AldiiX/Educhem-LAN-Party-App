import {NextResponse} from "next/server";
import type {NextRequest} from "next/server";
import {SYSTEM_DISABLED_PATH, SYSTEM_ENABLED} from "./config/system";
import {BACKEND_URL} from "./lib/vars";

const isProd = process.env.NODE_ENV === "production";
const accessCookieNames = isProd
    ? (["__Host-edlp_access", "edlp_access"] as const)
    : (["edlp_access", "__Host-edlp_access"] as const);
const refreshCookieNames = isProd
    ? (["__Host-edlp_refresh", "edlp_refresh"] as const)
    : (["edlp_refresh", "__Host-edlp_refresh"] as const);

function getCookie(request: NextRequest, names: readonly string[]) {
    for(const name of names) {
        const value = request.cookies.get(name)?.value;
        if(value) return value;
    }
    return null;
}

function accessTokenNeedsRefresh(token: string | null) {
    if(!token) return true;

    try {
        const parts = token.split(".");
        if(parts.length !== 3) return true;
        const payload = parts[1];
        if(!payload) return true;
        
        let jsonStr: string;
        if(typeof Buffer !== "undefined") {
            jsonStr = Buffer.from(payload, "base64url").toString("utf-8");
        } else {
            const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
            const padded = normalized.padEnd(normalized.length + (4 - (normalized.length % 4)) % 4, "=");
            jsonStr = atob(padded);
        }

        const decoded = JSON.parse(jsonStr) as {exp?: number};
        return typeof decoded.exp !== "number" || decoded.exp <= Math.floor(Date.now() / 1000) + 60;
    } catch {
        return true;
    }
}

function getSetCookieHeaders(headers: Headers) {
    const extended = headers as Headers & {getSetCookie?: () => string[]};
    if(extended.getSetCookie) return extended.getSetCookie();
    const single = headers.get("set-cookie");
    return single ? [single] : [];
}

function applySetCookieToRequest(request: NextRequest, setCookie: string) {
    const pair = setCookie.split(";", 1)[0];
    const separator = pair.indexOf("=");
    if(separator < 1) return;

    const name = pair.slice(0, separator);
    const value = pair.slice(separator + 1);
    if(!value || /(?:^|;)\s*max-age=0(?:;|$)/i.test(setCookie)) {
        request.cookies.delete(name);
        return;
    }

    request.cookies.set(name, value);
}

function buildCookieHeader(request: NextRequest) {
    return request.cookies.getAll().map(cookie => `${cookie.name}=${cookie.value}`).join("; ");
}

async function fetchAuth(path: string, request: NextRequest) {
    return fetch(`${BACKEND_URL}${path}`, {
        method: path.endsWith("/csrf") ? "GET" : "POST",
        cache: "no-store",
        headers: {
            cookie: buildCookieHeader(request),
        },
    });
}

async function refreshAuthentication(request: NextRequest) {
    const forwardedSetCookies: string[] = [];
    const refreshResponse = await fetchAuth("/api/v1/auth/refresh", request);
    for(const setCookie of getSetCookieHeaders(refreshResponse.headers)) {
        forwardedSetCookies.push(setCookie);
        applySetCookieToRequest(request, setCookie);
    }

    if(refreshResponse.ok) {
        const csrfResponse = await fetchAuth("/api/v1/auth/csrf", request);
        for(const setCookie of getSetCookieHeaders(csrfResponse.headers)) {
            forwardedSetCookies.push(setCookie);
            applySetCookieToRequest(request, setCookie);
        }
    }

    const response = NextResponse.next({request});
    for(const setCookie of forwardedSetCookies) response.headers.append("set-cookie", setCookie);
    return response;
}

export async function proxy(request: NextRequest) {
    const {pathname} = request.nextUrl;

    if (!SYSTEM_ENABLED && pathname !== SYSTEM_DISABLED_PATH) {
        return NextResponse.redirect(new URL(SYSTEM_DISABLED_PATH, request.url));
    }

    if (SYSTEM_ENABLED && pathname === SYSTEM_DISABLED_PATH) {
        return NextResponse.redirect(new URL("/app", request.url));
    }

    const accessToken = getCookie(request, accessCookieNames);
    const refreshToken = getCookie(request, refreshCookieNames);
    if(!refreshToken || !accessTokenNeedsRefresh(accessToken)) return NextResponse.next();

    try {
        return await refreshAuthentication(request);
    } catch {
        return NextResponse.next();
    }
}

export const config = {
    matcher: ["/app/:path*"],
};
