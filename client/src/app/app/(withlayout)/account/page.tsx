import type { Metadata } from 'next';
import AccountOverview from "./client";
import {useAccountPageContext} from "@/app/app/(withlayout)/account/layoutclient";

export const metadata: Metadata = {
    title: "Můj účet",
};

export default function AccountPage() {
    return <AccountOverview />;
}
