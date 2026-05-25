import type {Metadata} from "next";
import {requireLoggedAccountOrRedirect} from "@/lib/auth";
import SupportClient from "./client";

export const metadata: Metadata = {
    title: "Nahlásit problém",
};

export default async function SupportPage() {
    await requireLoggedAccountOrRedirect();

    return <SupportClient />;
}
