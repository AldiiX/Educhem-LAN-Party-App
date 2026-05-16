import type {Metadata} from "next";
import {requireLoggedAccountOrRedirect} from "@/lib/auth";
import ProblemClient from "./client";

export const metadata: Metadata = {
    title: "Nahlásit problém",
};

export default async function ProblemPage() {
    await requireLoggedAccountOrRedirect();

    return <ProblemClient />;
}
