# syntax=docker/dockerfile:1.7

ARG APP_UID=1000
ARG BUILD_CONFIGURATION=Release
ARG NODE_MAJOR=24

# frontend build stage
FROM node:${NODE_MAJOR}-bookworm-slim AS client-build
WORKDIR /src/client

COPY client/package*.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci --no-audit --no-fund

COPY client/ ./
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build \
    && test -f .next/standalone/server.js \
    && test -d .next/static

# backend build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION

WORKDIR /src

COPY ["server/server.csproj", "server/"]
COPY ["client/client.esproj", "client/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "./server/server.csproj"

COPY . .

WORKDIR /src/server

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build "./server.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/build \
    /p:ShouldRunNpmInstall=false \
    --no-restore

# backend publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION
ARG ENV_CACHE_BUST=0

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "./server.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:ShouldRunNpmInstall=false \
    /p:BuildCommand=true \
    --no-restore

# vytvoreni .env primo do publish vystupu
RUN --mount=type=secret,id=BACKEND_ENV_B64 \
    set -eu; \
    echo "info: env cache bust ${ENV_CACHE_BUST}"; \
    if [ -f /run/secrets/BACKEND_ENV_B64 ] && [ -s /run/secrets/BACKEND_ENV_B64 ]; then \
        echo "info: vytvarim /app/publish/.env z BACKEND_ENV_B64 secretu"; \
        base64 -d /run/secrets/BACKEND_ENV_B64 > /app/publish/.env; \
    else \
        echo "warn: BACKEND_ENV_B64 secret nebyl predan nebo je prazdny, vytvarim prazdny /app/publish/.env"; \
        : > /app/publish/.env; \
    fi; \
    chmod 600 /app/publish/.env

# final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG APP_UID

WORKDIR /app

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates nginx \
    && rm -rf /var/lib/apt/lists/*

COPY --from=client-build /usr/local/bin/node /usr/local/bin/node
COPY --from=publish /app/publish ./
COPY --from=client-build /src/client/.next/standalone /app/client
COPY --from=client-build /src/client/.next/static /app/client/.next/static
COPY --from=client-build /src/client/public /app/client/public

COPY nginx.conf /etc/nginx/nginx.conf
COPY --chmod=0755 start.sh ./start.sh

RUN chown -R "$APP_UID:$APP_UID" /app \
    && chmod 600 /app/.env

ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_ENVIRONMENT=Production \
    ASPNETCORE_ENVIRONMENT=Production \
    NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0

EXPOSE 80
CMD ["./start.sh"]
