import {Metadata} from "next";
import {getCachedCurrentLoggedAccount} from "@/lib/auth";
import {redirect} from "next/navigation";
import Client from "@/app/app/(withlayout)/administration/client";

export const metadata: Metadata = {
    title: "Administrace",
}

export default async function() {
    const account = await getCachedCurrentLoggedAccount();
    if(!account) redirect("/app");
    if(account.accountType !== "TeacherOrg" && account.accountType !== "SuperAdmin" && account.accountType !== "Admin") {
        redirect("/app");
    }

    return <Client />
}
