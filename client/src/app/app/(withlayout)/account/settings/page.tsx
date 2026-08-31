import type {Metadata} from "next";
import AccountSettings from "@/app/app/(withlayout)/account/settings/client";

export const metadata: Metadata = {
    title: "Nastavení účtu",
};

export default function AccountSettingsPage() {
    return <AccountSettings />;
}
