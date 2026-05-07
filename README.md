# ASP.NET + Next.js Template

This repository provides a **full-stack template** that combines **ASP.NET Core** for the backend and **Next.js** for the frontend.  
It is designed for rapid development of modern web applications with a clear separation of concerns between API and UI.

---

## 🚀 Features

- **Frontend (Next.js + TypeScript)**
  - Next.js with App Router
  - Pages, middleware, composables
  - SCSS global and per-page styles
  - Static assets in `public/`
  - Configurable via `next.config.ts`
  - Built-in theme management with `WebThemeProvider.tsx`

- **Backend (ASP.NET Core)**
  - Minimal API setup with controllers in `API/`
  - Configurable via `appsettings.json`
  - Ready for dependency injection and services
  - Dockerfile for containerized builds

- **Infrastructure**
  - `nginx.conf` for reverse proxy setup
  - Root `Dockerfile` for combined builds
  - `start.sh` script for deployment automation
