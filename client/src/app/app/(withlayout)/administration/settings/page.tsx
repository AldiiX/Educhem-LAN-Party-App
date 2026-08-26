import type {Metadata} from "next";
import {AppSettingsTab} from "../_components/tabs/AppSettingsTab";
import {requireAdministrationRole} from "../_lib/requireAdministrationRole";

export const metadata: Metadata = {
    title: "Nastavení aplikace • Administrace",
};

export default async function AdministrationSettingsPage() {
    await requireAdministrationRole("Admin");
    return <AppSettingsTab />;
}
