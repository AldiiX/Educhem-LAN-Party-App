import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    // set reverse proxy for api calls
    async rewrites() {
        return [
            {
                source: '/api/:path*',
                destination: 'http://localhost:8080/api/:path*' // Proxy to Backend
            }
        ]
    },
};

export default nextConfig;