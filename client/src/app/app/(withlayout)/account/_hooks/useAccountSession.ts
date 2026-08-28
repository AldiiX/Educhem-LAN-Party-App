"use client";

import {useRouter} from "next/navigation";
import {Account} from "@/schemas/AccountSchema";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {useWebTheme} from "@/app/_providers/WebThemeProvider";
import {apiFetch} from "@/lib/apiClient";

export function useAccountSession(initialAccount: Account) {
    const router = useRouter();
    const {account: authAccount, setAccount} = useAuth();
    const {toggleTheme} = useWebTheme();
    const account = authAccount ?? initialAccount;

    async function logout() {
        await apiFetch("/api/v1/auth/logout", {method: "POST"});
        setAccount(null);
        router.push("/app/login");
        router.refresh();
    }

    return {
        account,
        setAccount,
        router,
        logout,
        toggleTheme,
    };
}
