import type {Metadata} from "next";
import {LogsTab} from "../_components/tabs/LogsTab";
import {requireAdministrationRole} from "../_lib/requireAdministrationRole";

export const metadata: Metadata = {
    title: "Bezpečnostní logy • Administrace",
};

export default async function AdministrationLogsPage() {
    await requireAdministrationRole("Admin");
    return <LogsTab />;
}
