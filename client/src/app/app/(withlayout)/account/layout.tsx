import type {ReactNode} from "react";
import {requireLoggedAccountOrRedirect} from "@/lib/auth";
import AccountLayoutClient from "./layoutclient";

export default async function AccountLayout({children}: {children: ReactNode}) {
    const account = await requireLoggedAccountOrRedirect();

    return <AccountLayoutClient initialAccount={account}>{children}</AccountLayoutClient>;
}
