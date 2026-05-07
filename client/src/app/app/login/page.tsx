import type { Metadata } from 'next';
import Client from "@/app/app/login/client";

export const metadata: Metadata = {
    title: "Login"
}

export default function() {
    return <Client />
}