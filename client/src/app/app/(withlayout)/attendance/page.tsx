import type {Metadata} from "next";
import {requireLoggedAccountOrRedirect} from "@/lib/auth";
import AttendanceClient from "./client";

export const metadata: Metadata = {
    title: "Docházka",
};

export default async function AttendancePage() {
    await requireLoggedAccountOrRedirect();

    return <AttendanceClient />;
}
