import type {Metadata} from "next";
import {cookies} from "next/headers";
import {Users} from "./client";

export const metadata: Metadata = {
    title: "Uživatelé • Administrace",
};

export default async function AdministrationUsersPage() {
    const cookieStore = await cookies();
    const initialFiltersCollapsed = cookieStore.get("administrationUsersFiltersCollapsed")?.value === "true";

    return <Users initialFiltersCollapsed={initialFiltersCollapsed} />;
}
