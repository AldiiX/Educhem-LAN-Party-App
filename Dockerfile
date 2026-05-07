# Define the user ID (default value 1000)
ARG APP_UID=1000

# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Stage to install Node.js
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS with-node
RUN apt-get update && apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_24.x | bash && apt-get install -y nodejs

# Stage to build the backend
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["server/server.csproj", "server/"]
COPY ["client/client.esproj", "client/"]
RUN dotnet restore "./server/server.csproj"
COPY . .
WORKDIR "/src/server"

# vytvoreni prazdnyho .env souboru v tomto direktory pokud .env neexistuje
RUN if [ ! -f ".env" ]; then touch .env; fi

# buildnuti backendu
RUN dotnet build "./server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage to publish the backend
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Switch to root to install packages
USER root
RUN apt-get update && apt-get install -y curl nginx
RUN curl -sL https://deb.nodesource.com/setup_24.x | bash && apt-get install -y nodejs

# Copy frontend files and fix permissions
COPY ["client/", "/app/client/"]
RUN chown -R $APP_UID:$APP_UID /app/client
WORKDIR /app/client

# Set npm cache and install dependencies
RUN npm config set cache /app/.npm
RUN npm install --unsafe-perm

# Build the frontend
RUN npm run build

# Copy Nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Switch back to non-privileged user
#USER $APP_UID

# Prepare the start script
EXPOSE 80
WORKDIR /app
COPY --chmod=0755 start.sh .
CMD ["./start.sh"]