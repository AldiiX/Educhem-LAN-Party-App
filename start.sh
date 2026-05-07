#!/bin/sh

# Start the backend in the background
dotnet server.dll &

# Start the frontend (vite preview) in the background
cd /app/client/
npm run start &

# Start Nginx
nginx -g 'daemon off;'