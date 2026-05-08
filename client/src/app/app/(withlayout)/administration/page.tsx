import {Metadata} from "next";
import {getCachedCurrentLoggedAccount} from "@/lib/auth";
import {redirect} from "next/navigation";
import Client from "@/app/app/(withlayout)/administration/client";
import {fetchBackendJson} from "@/lib/backendClient";
import {AccountSchema} from "@/schemas/AccountSchema";

export const metadata: Metadata = {
    title: "Administrace",
}

export default async function() {
    const account = await getCachedCurrentLoggedAccount();
    if(!account) redirect("/app");
    if(account.accountType !== "TeacherOrg" && account.accountType !== "SuperAdmin" && account.accountType !== "Admin") {
        redirect("/app");
    }

    const response = await fetchBackendJson<unknown>("/api/v1/account/all", {method: "GET", cache: "no-cache"});
    const accounts = AccountSchema.array().parse(response ?? []);

    return <Client accounts={accounts} />
}
