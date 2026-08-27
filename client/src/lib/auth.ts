import "server-only";
import {cache} from "react";
import {redirect} from "next/navigation";
import {fetchBackendJson} from "./backendClient";
import {Account, AccountSchema} from "@/schemas/AccountSchema";

export const getCurrentLoggedAccount = cache(async (): Promise<Account | null> => {
    // backend musi vracet json kdyz je uzivatel prihlaseny, jinak 401/403/204
    let res;
    try {
        res = await fetchBackendJson<unknown>("/api/v1/account", {method: "GET", cache: "no-cache"});
    } catch {
        return null;
    }

    const result = AccountSchema.safeParse(res);
    if (!result.success) return null;

    return result.data;
});

export const getCachedCurrentLoggedAccount = getCurrentLoggedAccount;

export async function requireLoggedAccountOrRedirect(): Promise<Account> {
    const user = await getCurrentLoggedAccount();
    if (!user) {
        redirect("/app/login");
    }
    return user;
}