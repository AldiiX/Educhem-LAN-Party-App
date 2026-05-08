"use client"

import {useAuth} from "@/app/app/_providers/AuthProvider";

export default function() {
    const { account } = useAuth();
    if(!account) return null;

    return <>
        <h1>{ account.fullName }</h1>
    </>
}