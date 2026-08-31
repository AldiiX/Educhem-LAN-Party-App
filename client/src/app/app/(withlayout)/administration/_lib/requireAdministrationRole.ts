import "server-only";
import {redirect} from "next/navigation";
import {getCachedCurrentLoggedAccount} from "@/lib/auth";
import {hasRoleAtLeast} from "@/lib/roles";
import type {AccountType} from "@/schemas/AccountSchema";

export async function requireAdministrationRole(role: AccountType) {
    const account = await getCachedCurrentLoggedAccount();
    if(!account || !hasRoleAtLeast(account, role)) redirect("/app");
    return account;
}
