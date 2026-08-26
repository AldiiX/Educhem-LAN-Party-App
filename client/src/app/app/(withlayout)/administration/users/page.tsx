import type {Metadata} from "next";
import {UsersTab} from "../_components/tabs/UsersTab";
import {requireAdministrationRole} from "../_lib/requireAdministrationRole";

export const metadata: Metadata = {
    title: "Uživatelé • Administrace",
};

export default async function AdministrationUsersPage() {
    await requireAdministrationRole("TeacherOrg");
    return <UsersTab />;
}
