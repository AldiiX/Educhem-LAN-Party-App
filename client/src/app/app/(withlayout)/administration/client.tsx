"use client"

import style from "./client.module.scss";
import {useState} from "react";
import {UsersTab} from "./_components/tabs/UsersTab";
import {ReservationsTab} from "./_components/tabs/ReservationsTab";
import {ForumPostsTab} from "./_components/tabs/ForumPostsTab";
import {LogsTab} from "./_components/tabs/LogsTab";
import {AppSettingsTab} from "./_components/tabs/AppSettingsTab";
import {AchievementsTab} from "./_components/tabs/AchievementsTab";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {hasRoleAtLeast} from "@/lib/roles";

type AdministrationTabKey = "users" | "reservations" | "forum" | "logs" | "settings" | "achievements";
type AdministrationTab = {
    key: AdministrationTabKey;
    label: string;
};

export default function AdministrationClient() {
    const {account} = useAuth();
    const canManageApp = hasRoleAtLeast(account, "Admin");
    const [activeTab, setActiveTab] = useState<AdministrationTabKey>("users");
    const tabs: AdministrationTab[] = [
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

        <div className={style.tabs}>
            {tabs.map(tab => (
                <button
                    key={tab.key}
                    type="button"
                    className={activeTab === tab.key ? style.active : ""}
                    onClick={() => setActiveTab(tab.key)}
                >
                    {tab.label}
                </button>
            ))}
        </div>

        {activeTab === "users" && <UsersTab />}
        {activeTab === "reservations" && <ReservationsTab />}
        {activeTab === "forum" && <ForumPostsTab />}
        {activeTab === "logs" && <LogsTab />}
        {activeTab === "settings" && <AppSettingsTab />}
        {activeTab === "achievements" && <AchievementsTab />}
    </main>
}
