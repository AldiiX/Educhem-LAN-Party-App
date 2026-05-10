# syntax=docker/dockerfile:1.7

ARG APP_UID=1000
ARG BUILD_CONFIGURATION=Release

# runtime stage pro backend
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

# sdk stage s nodejs pro pripadne msbuild/js kroky
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-with-node
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && curl -fsSL https://deb.nodesource.com/setup_24.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# frontend build stage
FROM node:24-bookworm-slim AS client-build
WORKDIR /src/client

COPY client/package*.json ./
RUN npm ci --unsafe-perm --no-audit --no-fund

COPY client/ ./
RUN npm run build

# backend build stage
FROM dotnet-with-node AS build
ARG BUILD_CONFIGURATION

WORKDIR /src

COPY ["server/server.csproj", "server/"]
COPY ["client/client.esproj", "client/"]

RUN dotnet restore "./server/server.csproj"

COPY . .

WORKDIR /src/server

RUN dotnet build "./server.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/build \
    --no-restore

# backend publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION

RUN dotnet publish "./server.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# vytvoreni .env primo do publish vystupu
RUN --mount=type=secret,id=BACKEND_ENV_B64 \
    set -eu; \
    if [ -f /run/secrets/BACKEND_ENV_B64 ] && [ -s /run/secrets/BACKEND_ENV_B64 ]; then \
        echo "info: vytvarim /app/publish/.env z BACKEND_ENV_B64 secretu"; \
        base64 -d /run/secrets/BACKEND_ENV_B64 > /app/publish/.env; \
    else \
        echo "warn: BACKEND_ENV_B64 secret nebyl predan nebo je prazdny, vytvarim prazdny /app/publish/.env"; \
        : > /app/publish/.env; \
    fi; \
    chmod 600 /app/publish/.env

# final image
FROM base AS final
ARG APP_UID

WORKDIR /app

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish ./
COPY --from=client-build /src/client/dist /app/client/dist

COPY nginx.conf /etc/nginx/nginx.conf
COPY --chmod=0755 start.sh ./start.sh

RUN chown -R "$APP_UID:$APP_UID" /app \
    && chmod 600 /app/.env

CMD ["./start.sh"]