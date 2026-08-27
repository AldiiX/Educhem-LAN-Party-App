import type {Metadata} from "next";
import {AppSettings} from "./client";

export const metadata: Metadata = {
    title: "Nastavení aplikace • Administrace",
};

export default function AdministrationSettingsPage() {
    return <AppSettings />;
}