import { Metadata } from 'next'
import Client from "@/app/app/(withlayout)/profile/client";
import {getCachedCurrentLoggedAccount} from "@/lib/auth";

export const metadata: Metadata = {
    title: 'Veřejný profil',
}

export default async function( ){
    const account = await getCachedCurrentLoggedAccount();

    if(!account) return null;

    return <Client account={account} />
}