"use client";

import React, {
    createContext,
    useContext,
    useEffect,
    useMemo,
    useState,
} from "react";
import type { ResolvedWebTheme, WebTheme } from "@/app/_types";

type WebThemeContextValue = {
    theme: WebTheme;
    resolvedTheme: ResolvedWebTheme;
    setTheme: (theme: WebTheme) => void;
    toggleTheme: () => void;
};

type WebThemeProviderProps = {
    initialTheme: WebTheme;
    initialResolvedTheme: ResolvedWebTheme;
    children: React.ReactNode;
};

const WebThemeContext = createContext<WebThemeContextValue | null>(null);

function getSystemTheme(): ResolvedWebTheme {
    if(typeof window === "undefined") {
        return "light";
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light";
}

function resolveTheme(theme: WebTheme): ResolvedWebTheme {
    if(theme === "auto") {
        return getSystemTheme();
    }

    return theme;
}

function setThemeCookie(theme: WebTheme) {
    document.cookie = `webTheme=${theme}; path=/; max-age=31536000; samesite=lax`;
}

function applyTheme(theme: ResolvedWebTheme) {
    document.documentElement.dataset.theme = theme;
}

export function WebThemeProvider({
                                     initialTheme,
                                     initialResolvedTheme,
                                     children,
                                 }: WebThemeProviderProps) {
    const [theme, setThemeState] = useState<WebTheme>(initialTheme);
    const [resolvedTheme, setResolvedTheme] = useState<ResolvedWebTheme>(initialResolvedTheme);

    useEffect(() => {
        const nextResolvedTheme = resolveTheme(theme);

        setResolvedTheme(nextResolvedTheme);
        applyTheme(nextResolvedTheme);

        if(theme !== "auto") {
            return;
        }

        const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

        const handleSystemThemeChange = () => {
            const nextSystemTheme = resolveTheme("auto");

            setResolvedTheme(nextSystemTheme);
            applyTheme(nextSystemTheme);
        };

        mediaQuery.addEventListener("change", handleSystemThemeChange);

        return () => {
            mediaQuery.removeEventListener("change", handleSystemThemeChange);
        };
    }, [theme]);

    const setTheme = (nextTheme: WebTheme) => {
        setThemeState(nextTheme);
        setThemeCookie(nextTheme);
    };

    const toggleTheme = () => {
        setTheme(resolvedTheme === "dark" ? "light" : "dark");
    };

    const value = useMemo<WebThemeContextValue>(() => ({
        theme,
        resolvedTheme,
        setTheme,
        toggleTheme,
    }), [theme, resolvedTheme]);

    return (
        <WebThemeContext.Provider value={value}>
            {children}
        </WebThemeContext.Provider>
    );
}

export function useWebTheme() {
    const context = useContext(WebThemeContext);

    if(context === null) {
        throw new Error("useWebTheme must be used inside WebThemeProvider");
    }

    return context;
}