import process from "process";

export const isProd = process.env.NODE_ENV === "production";
export const BACKEND_URL = isProd ? "http://localhost:8080" : "http://localhost:8080";