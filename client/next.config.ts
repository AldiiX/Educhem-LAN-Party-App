import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    output: "standalone",
    // set reverse proxy for api calls
    async rewrites() {
        return [
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
