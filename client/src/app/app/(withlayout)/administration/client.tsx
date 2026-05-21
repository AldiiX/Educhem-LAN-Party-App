"use client"

import style from "./client.module.scss";
import {useState} from "react";
import {UsersTab} from "./_components/tabs/UsersTab";
import {ReservationsTab} from "./_components/tabs/ReservationsTab";
import {ForumPostsTab} from "./_components/tabs/ForumPostsTab";
import {LogsTab} from "./_components/tabs/LogsTab";
import {AppSettingsTab} from "./_components/tabs/AppSettingsTab";
import {AchievementsTab} from "./_components/tabs/AchievementsTab";

export default function AdministrationClient() {
    const [activeTab, setActiveTab] = useState<"users" | "reservations" | "forum" | "logs" | "settings" | "achievements">("users");
    const tabs = [
        { key: "users", label: "Uživatelé" },
        { key: "reservations", label: "Rezervace" },
        { key: "forum", label: "Forum příspěvky" },
        { key: "logs", label: "Bezpečnostní logy" },
        { key: "settings", label: "Nastavení aplikace" },
        { key: "achievements", label: "Správa ocenění" },
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
