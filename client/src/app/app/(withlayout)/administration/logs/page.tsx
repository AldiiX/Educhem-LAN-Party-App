import type {Metadata} from "next";
import {cookies} from "next/headers";
import {Logs} from "./client";

export const metadata: Metadata = {
    title: "Bezpečnostní logy • Administrace",
};

export default async function AdministrationLogsPage() {
    const cookieStore = await cookies();
    const initialFiltersCollapsed = cookieStore.get("administrationLogsFiltersCollapsed")?.value === "true";

    return <Logs initialFiltersCollapsed={initialFiltersCollapsed} />;
}

