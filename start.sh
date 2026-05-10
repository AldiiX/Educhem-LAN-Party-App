#!/bin/sh
set -eu

export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:8080}"
export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Production}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-$DOTNET_ENVIRONMENT}"
export NODE_ENV="${NODE_ENV:-production}"
export PORT="${PORT:-3000}"
export HOSTNAME="${HOSTNAME:-0.0.0.0}"

if [ -s /app/.env ]; then
    echo "info: /app/.env existuje a neni prazdny"
else
    echo "warn: /app/.env neexistuje nebo je prazdny"
fi

if [ ! -f /app/server.dll ]; then
    echo "error: /app/server.dll neexistuje"
    exit 1
fi

if [ ! -f /app/client/package.json ]; then
    echo "error: /app/client/package.json neexistuje"
    exit 1
fi

terminate() {
    echo "info: ukoncuju procesy"
    kill -TERM "$backend_pid" "$frontend_pid" "$nginx_pid" 2>/dev/null || true
    wait "$backend_pid" "$frontend_pid" "$nginx_pid" 2>/dev/null || true
}

trap terminate INT TERM

echo "info: startuju backend na ${ASPNETCORE_URLS}"
dotnet /app/server.dll &
backend_pid=$!

echo "info: startuju frontend na 0.0.0.0:${PORT}"
cd /app/client
npm run start &
frontend_pid=$!

echo "info: startuju nginx na portu 80"
nginx -g 'daemon off;' &
nginx_pid=$!

while true; do
    if ! kill -0 "$backend_pid" 2>/dev/null; then
        echo "error: backend proces skoncil"
        terminate
        exit 1
    fi

    if ! kill -0 "$frontend_pid" 2>/dev/null; then
        echo "error: frontend proces skoncil"
        terminate
        exit 1
    fi

    if ! kill -0 "$nginx_pid" 2>/dev/null; then
        echo "error: nginx proces skoncil"
        terminate
        exit 1
    fi

    sleep 2
done