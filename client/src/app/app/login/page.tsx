import type { Metadata } from 'next';
import Client from "@/app/app/login/client";
import {getCurrentLoggedAccount} from "@/lib/auth";
import { redirect } from "next/navigation"

export const metadata: Metadata = {
    title: "Login"
}

export default async function() {
    const account = await getCurrentLoggedAccount();
    if(account) redirect("/app/")

    return <Client />
}