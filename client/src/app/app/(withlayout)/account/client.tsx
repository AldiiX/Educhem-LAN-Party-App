"use client";

import styles from "./client.module.scss";
import {Avatar} from "@/components/Avatar";
import {Button} from "@/components/Button";
import {Account} from "@/schemas/AccountSchema";
import {accountTypeLabel} from "@/lib/enumLabels";
import {useAccountPageContext} from "@/app/app/(withlayout)/account/layoutclient";

export default function AccountOverview() {
    const {account, logout, toggleTheme} = useAccountPageContext();
    
    return <section className={styles.overview}>
        <Avatar name={account.fullName} src={account.avatarUrl} size="200px" className={styles.avatar} />
        <h2>{account.fullName}</h2>
        <p className={styles.email}>{account.email}</p>
        <p className={styles.role}>{accountTypeLabel(account.accountType, account.gender)}</p>

        <div className={styles.actions}>
            <Button type="secondary" text="Změnit theme" icon="/icons/theme.svg" onClick={toggleTheme} />
            <Button type="primary" text="Odhlásit" icon="/icons/logout.svg" onClick={logout} />
        </div>
    </section>;
}
