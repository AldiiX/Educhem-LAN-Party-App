import type {ReactNode} from "react";
import AdministrationClient from "./layoutclient";
import {requireAdministrationRole} from "./_lib/requireAdministrationRole";

export default async function AdministrationLayout({children}: {children: ReactNode}) {
    await requireAdministrationRole("TeacherOrg");

    return <AdministrationClient>{children}</AdministrationClient>;
}

