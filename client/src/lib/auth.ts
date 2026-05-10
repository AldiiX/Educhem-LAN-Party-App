import {redirect} from "next/navigation";
import {fetchBackendJson} from "./backendClient";
import {Account, AccountSchema} from "@/schemas/AccountSchema";
import "server-only";
import {cache} from "react";

export async function getCurrentLoggedAccount(): Promise<Account | null> {
    // backend musi vracet json kdyz je uzivatel prihlaseny, jinak 401/403/204
    let res;
    try {
        res = await fetchBackendJson<any>("/api/v1/account", {method: "GET", cache: "no-cache"});
    }catch (e){
        return null;
    }

    const result = AccountSchema.safeParse(res);
    //console.log(result);
    if (! result.success) return null;

    //console.log(res);
    return result.data;
}

export async function requireLoggedAccountOrRedirect(): Promise<Account> {
    const user = await getCachedCurrentLoggedAccount();
    if (!user) {
        redirect("/app/login");
    }
    return user;
}

export const getCachedCurrentLoggedAccount = cache(async () => {
    return await getCurrentLoggedAccount();
});