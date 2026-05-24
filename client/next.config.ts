import { createHash } from "crypto";
import type { NextConfig } from "next";
import packageJson from "./package.json";
import { trapRedirectSources, type WebpackRule } from "./next.config.types";

function getHashedCssModuleClass(context: { resourcePath: string }, _localIdentName: string, localName: string) {
    const hash = createHash("sha256")
        .update(`${context.resourcePath}:${localName}`)
        .digest("base64url")
        .slice(0, 11);
    const firstLetter = String.fromCharCode(97 + (hash.charCodeAt(0) % 26));

    return `${firstLetter}${hash}`;
}

function applyProductionCssModuleHashing(rule: WebpackRule) {
    for (const nestedRule of rule.oneOf ?? []) {
        applyProductionCssModuleHashing(nestedRule);
    }

    for (const nestedRule of rule.rules ?? []) {
        applyProductionCssModuleHashing(nestedRule);
    }

    if (!Array.isArray(rule.use)) {
        return;
    }

    for (const loader of rule.use) {
        if (!loader.loader?.includes("css-loader") || !loader.options?.modules?.getLocalIdent) {
            continue;
        }

        loader.options.modules.getLocalIdent = getHashedCssModuleClass;
    }
}

const nextConfig: NextConfig = {
    output: "standalone",
    env: {
        NEXT_PUBLIC_APP_VERSION: packageJson.version,
    },

    webpack(config, { dev }) {
        if (!dev) {
            for (const rule of config.module.rules as WebpackRule[]) {
                applyProductionCssModuleHashing(rule);
            }
        }

        return config;
    },

    // set reverse proxy for api calls - pouze v devu
    async rewrites() {
        return [
            ...trapRedirectSources.map((source) => ({
                source,
                destination: "/__nice-try",
            })),

            {
                source: '/api/:path*',
                destination: 'http://localhost:8080/api/:path*' // Proxy to Backend
            },

            {
                source: '/hubs/:path*',
                destination: 'http://localhost:8080/hubs/:path*' // Proxy to Backend
            }
        ]
    },

    //reactStrictMode: false, // pouze pro dev test
};

export default nextConfig;
