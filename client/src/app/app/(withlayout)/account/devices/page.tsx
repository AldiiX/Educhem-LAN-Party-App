import type {Metadata} from "next";
import AccountDevices from "@/app/app/(withlayout)/account/devices/client";

export const metadata: Metadata = {
    title: "Přihlášená zařízení",
};

export default function AccountDevicesPage() {
    return <AccountDevices />;
}
