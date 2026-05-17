import {NextResponse} from "next/server";
import type {NextRequest} from "next/server";
import {SYSTEM_DISABLED_PATH, SYSTEM_ENABLED} from "./config/system";

export function proxy(request: NextRequest) {
    const {pathname} = request.nextUrl;

    if (!SYSTEM_ENABLED && pathname !== SYSTEM_DISABLED_PATH) {
        return NextResponse.redirect(new URL(SYSTEM_DISABLED_PATH, request.url));
    }

    if (SYSTEM_ENABLED && pathname === SYSTEM_DISABLED_PATH) {
        return NextResponse.redirect(new URL("/app", request.url));
    }

    return NextResponse.next();
}

export const config = {
    matcher: ["/app/:path*"],
};
