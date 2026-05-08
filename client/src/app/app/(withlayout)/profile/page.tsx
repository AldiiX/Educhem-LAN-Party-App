import { Metadata } from 'next'
import {getCachedCurrentLoggedAccount, getCurrentLoggedAccount} from "@/lib/auth";
import { redirect } from "next/navigation";
import Client from "@/app/app/(withlayout)/profile/client";

export const metadata: Metadata = {
    title: 'Profil',
}

export default async function( ){
    const account = await getCachedCurrentLoggedAccount();
    if(!account) redirect("/app")

    return <Client />
}