import {getCachedCurrentLoggedAccount, getCurrentLoggedAccount} from "@/lib/auth";
import Client from "@/app/app/(withlayout)/profile/client";
import { redirect } from "next/navigation";


export default async function({ children }: { children: React.ReactNode}) {
    const account = await getCachedCurrentLoggedAccount();
    if(!account) redirect("/app")

    return children;
}