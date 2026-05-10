import type {Metadata} from "next";
import {requireLoggedAccountOrRedirect} from "@/lib/auth";
import AccountClient from "./client";

export const metadata: Metadata = {
    title: "Můj účet",
};

export default async function AccountPage() {
    const account = await requireLoggedAccountOrRedirect();
    return <AccountClient initialAccount={account} />;
}
