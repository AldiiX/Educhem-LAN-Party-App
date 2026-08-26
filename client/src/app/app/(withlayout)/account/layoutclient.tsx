"use client";

import styles from "./layoutclient.module.scss";
import Link from "next/link";
import {createContext, ReactNode, useContext, useEffect} from "react";
import {usePathname, useRouter, useSearchParams} from "next/navigation";
import toast from "react-hot-toast";
import {Account} from "@/schemas/AccountSchema";
import {useAccountPage} from "./_hooks/useAccountPage";
import {AccountModals} from "./_components/AccountModals";
import {platforms} from "@/data/platforms";

const AccountPageContext = createContext<ReturnType<typeof useAccountPage> | null>(null);

const tabs = [
    {href: "/app/account", label: "Přehled"},
    {href: "/app/account/achievements", label: "Achievementy"},
    {href: "/app/account/settings", label: "Nastavení"},
] as const;

export default function AccountLayoutClient({initialAccount, children}: {initialAccount: Account; children: ReactNode}) {
    const state = useAccountPage(initialAccount);
    const pathname = usePathname();
    const router = useRouter();
    const searchParams = useSearchParams();

    useEffect(() => {
        const platform = platforms.find(item => searchParams.has(item.id));
        if(!platform) return;

        const status = searchParams.get(platform.id);
        const errorMessage = {
            "already-linked": `${platform.name} účet už je propojený s jiným Educhem LAN Party účtem.`,
            cancelled: `Propojení s ${platform.name} bylo zrušeno.`,
            error: `Propojení s ${platform.name} se nepodařilo. Zkus to prosím znovu.`,
        }[status ?? ""];

        if(status === "linked") {
            toast.success(`${platform.name} účet byl propojen.`, {id: `oauth-${platform.id}-${status}`});
        } else if(errorMessage) {
            state.showPlatformError(errorMessage);
        }

        router.replace("/app/account/settings", {scroll: false});
    }, [router, searchParams, state.showPlatformError]);

    return <AccountPageContext.Provider value={state}>
        <main className={styles.accountPage}>
            <h1>Můj účet</h1>

            <nav className={styles.tabs} aria-label="Sekce účtu">
                {tabs.map(tab => <Link
                    key={tab.href}
                    href={tab.href}
                    className={pathname === tab.href ? styles.active : ""}
                    aria-current={pathname === tab.href ? "page" : undefined}
                >{tab.label}</Link>)}
            </nav>

            <div className={styles.tabContent}>{children}</div>

            <AccountModals state={state} />
        </main>
    </AccountPageContext.Provider>;
}

export function useAccountPageContext() {
    const state = useContext(AccountPageContext);
    if(!state) throw new Error("Account page context is unavailable.");
    return state;
}
