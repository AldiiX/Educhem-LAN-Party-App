import type { Metadata } from "next";
import "./globals.scss";
import { cookies } from "next/headers";
import type { ResolvedWebTheme, WebTheme } from "@/app/_types";
import { WebThemeProvider } from "@/app/_providers/WebThemeProvider";
import {siteConfig} from "@/data/site";

export const metadata: Metadata = {
    title: {
        default: "Domů » " + siteConfig.currentEvent.title,
        template: `%s » ${siteConfig.currentEvent.title}`,
    },
    description: `${siteConfig.currentEvent.title} na ${siteConfig.currentEvent.venueFull}. ${siteConfig.currentEvent.dateLong}, vstup ${siteConfig.currentEvent.fee}.`,
    robots: {
        index: false,
        follow: false,
        googleBot: {
            index: false,
            follow: false,
        },
    },
}

function normalizeTheme(value: string | undefined): WebTheme {
    if(value === "light" || value === "dark" || value === "auto") {
        return value;
    }

    return "auto";
}

function getServerResolvedTheme(theme: WebTheme): ResolvedWebTheme {
    if(theme === "dark") {
        return "dark";
    }

    return "light";
}

function createThemeInitScript(initialTheme: WebTheme) {
    return `
(function() {
    try {
        var theme = ${JSON.stringify(initialTheme)};

        if(theme !== "light" && theme !== "dark" && theme !== "auto") {
            theme = "auto";
        }

        var resolvedTheme = theme;

        if(theme === "auto") {
            resolvedTheme = window.matchMedia("(prefers-color-scheme: dark)").matches
                ? "dark"
                : "light";
        }

        document.documentElement.dataset.theme = resolvedTheme;
    } catch(error) {
        document.documentElement.dataset.theme = "light";
    }
})();
`;
}

export default async function RootLayout({
                                             children,
                                         }: Readonly<{
    children: React.ReactNode;
}>) {
    const cookieStore = await cookies();

    const initialTheme = normalizeTheme(cookieStore.get("webTheme")?.value);
    const initialResolvedTheme = getServerResolvedTheme(initialTheme);

    return (
        <html
            lang="en"
            data-theme={initialResolvedTheme}
            suppressHydrationWarning
        >
        <head>
            <script
                dangerouslySetInnerHTML={{
                    __html: createThemeInitScript(initialTheme),
                }}
            />
        </head>

        <body>
        <WebThemeProvider
            initialTheme={initialTheme}
            initialResolvedTheme={initialResolvedTheme}
        >
            {children}
        </WebThemeProvider>
        </body>
        </html>
    );
}