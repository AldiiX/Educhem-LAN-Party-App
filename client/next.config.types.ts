export type WebpackRule = {
    oneOf?: WebpackRule[];
    rules?: WebpackRule[];
    use?: WebpackUse;
};

export type WebpackUse =
    | WebpackLoader
    | WebpackLoader[]
    | ((...args: unknown[]) => WebpackLoader | WebpackLoader[]);

export type WebpackLoader = {
    loader?: string;
    options?: {
        modules?: {
            getLocalIdent?: (
                context: { resourcePath: string },
                localIdentName: string,
                localName: string,
            ) => string;
        };
    };
};
export const trapRedirectSources = [
    "/.env",
    "/app/.env",
    "/.env.local",
    "/.env.development",
    "/.env.production",
    "/.env.backup",
    "/.env.bak",
    "/.git",
    "/.git/:path*",
    "/.svn",
    "/.svn/:path*",
    "/wp-admin",
    "/wp-admin/:path*",
    "/wp-content/:path*",
    "/wp-includes/:path*",
    "/wp-login.php",
    "/xmlrpc.php",
    "/phpmyadmin",
    "/phpmyadmin/:path*",
    "/pma",
    "/pma/:path*",
    "/adminer.php",
    "/admin.php",
    "/config.php",
    "/config.json",
    "/backup.zip",
    "/backup.tar.gz",
    "/database.sql",
    "/dump.sql",
];
