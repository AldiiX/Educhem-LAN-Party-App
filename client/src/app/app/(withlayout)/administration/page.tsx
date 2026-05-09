import {Metadata} from "next";
import {getCachedCurrentLoggedAccount} from "@/lib/auth";
import {redirect} from "next/navigation";
import Client from "@/app/app/(withlayout)/administration/client";
import {hasRoleAtLeast} from "@/lib/roles";

export const metadata: Metadata = {
    title: "Administrace",
}

export default async function() {
    const account = await getCachedCurrentLoggedAccount();
    if(!account) redirect("/app");
    if(!hasRoleAtLeast(account, "TeacherOrg")) {
        redirect("/app");
    }

    return <Client />
}
