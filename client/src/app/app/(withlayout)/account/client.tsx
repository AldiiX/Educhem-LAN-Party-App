"use client";

import styles from "./client.module.scss";
import {Account} from "@/schemas/AccountSchema";
import {useAccountPage} from "./_hooks/useAccountPage";
import {AccountOverview} from "./_components/AccountOverview";
import {AccountSettings} from "./_components/AccountSettings";
import {AccountModals} from "./_components/AccountModals";
import {AccountAchievements} from "./_components/AccountAchievements";

export default function AccountClient({initialAccount}: {initialAccount: Account}) {
    const state = useAccountPage(initialAccount);

    return <main className={styles.accountPage}>
        <h1>Můj účet</h1>

        <div className={styles.tabs}>
            <button type="button" className={state.selectedTab === "overview" ? styles.active : ""} onClick={() => state.setSelectedTab("overview")}>Přehled</button>
            <button type="button" className={state.selectedTab === "achievements" ? styles.active : ""} onClick={() => state.setSelectedTab("achievements")}>Achievementy</button>
            <button type="button" className={state.selectedTab === "settings" ? styles.active : ""} onClick={() => state.setSelectedTab("settings")}>Nastavení</button>
        </div>

        {state.selectedTab === "overview" ? (
            <AccountOverview account={state.account} onToggleTheme={state.toggleTheme} onLogout={state.logout} />
        ) : state.selectedTab === "achievements" ? (
            <AccountAchievements account={state.account} state={state} />
        ) : (
            <AccountSettings state={state} />
        )}

        <AccountModals state={state} />
    </main>;
}
