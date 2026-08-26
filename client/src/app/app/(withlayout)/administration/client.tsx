"use client"

import style from "./client.module.scss";
import Link from "next/link";
import type {ReactNode} from "react";
import {usePathname} from "next/navigation";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {hasRoleAtLeast} from "@/lib/roles";

type AdministrationTabKey = "overview" | "users" | "reservations" | "forum" | "logs" | "settings" | "achievements";
type AdministrationTab = {
    key: AdministrationTabKey;
    label: string;
};

export default function AdministrationClient({children}: {children: ReactNode}) {
    const {account} = useAuth();
    const pathname = usePathname();
    const canManageApp = hasRoleAtLeast(account, "Admin");
    const tabs: AdministrationTab[] = [
        //{ key: "overview", label: "Přehled" },
        { key: "users", label: "Uživatelé" },
        // { key: "reservations", label: "Rezervace" },
        // { key: "forum", label: "Forum příspěvky" },
        ...(canManageApp ? [
            { key: "logs", label: "Bezpečnostní logy" },
            { key: "settings", label: "Nastavení aplikace" },
        ] satisfies AdministrationTab[] : []),
        // { key: "achievements", label: "Správa ocenění" },
    ] as const;

    return <main className={style.administration}>
        <h1>Administrace</h1>

        <nav className={style.tabs} aria-label="Sekce administrace">
            {tabs.map(tab => {
                const href = tab.key === "overview" ? "/app/administration" : `/app/administration/${tab.key}`;

                return <Link
                    key={tab.key}
                    href={href}
                    className={pathname === href ? style.active : ""}
                    aria-current={pathname === href ? "page" : undefined}
                >
                    {tab.label}
                </Link>;
            })}
        </nav>

        <div className={style.tabContent}>{children}</div>
    </main>
}
