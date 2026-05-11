"use client";

import Link from "next/link";
import type {CSSProperties} from "react";
import {Account} from "@/schemas/AccountSchema";
import {hasRoleAtLeast} from "@/lib/roles";
import type {HomeDashboard} from "./page";
import {useHomeGreeting} from "./_hooks/useHomeGreeting";
import styles from "./HomeClient.module.scss";
import {Avatar} from "@/components/Avatar";

type HomeClientProps = {
    account: Account | null;
    dashboard: HomeDashboard | null;
};

const quickLinks = [
    {href: "/app/reservations", label: "Rezervace", icon: "/icons/calc.svg"},
    {href: "/app/map", label: "Mapa", icon: "/icons/map.svg"},
    {href: "/app/chat", label: "Chat", icon: "/icons/chat.svg", authOnly: true},
    {href: "/app/tournaments", label: "Turnaje", icon: "/icons/trophy_star.svg"},
    {href: "/app/problem", label: "Nahlásit problém", icon: "/icons/warn2.svg"},
    {href: "/app/administration", label: "Administrace", icon: "/icons/user_with_shield.svg", staffOnly: true},
];

export default function HomeClient({account, dashboard}: HomeClientProps) {
    const greeting = useHomeGreeting(account?.firstName ?? null);
    const links = quickLinks.filter(link => {
        if(link.authOnly && !account) return false;
        if(link.staffOnly) return hasRoleAtLeast(account, "TeacherOrg");
        return true;
    });
    const totalAccounts = dashboard?.totalAccounts ?? 0;
    const reservationsEnabled = dashboard?.reservationsEnabled ?? 0;
    const reservationRatio = totalAccounts > 0 ? Math.round((reservationsEnabled / totalAccounts) * 100) : 0;
    const activeRatio = totalAccounts > 0 ? Math.round(((dashboard?.activeToday ?? 0) / totalAccounts) * 100) : 0;
    const maxClassCount = Math.max(...(dashboard?.classBreakdown.map(item => item.count) ?? [1]), 1);

    return <main className={styles.home}>
        <section className={styles.hero}>
            <h1>{greeting}</h1>
            <div className={styles.quickLinks}>
                {links.map(link => (
                    <Link key={link.href} href={link.href}>
                        <span style={{maskImage: `url(${link.icon})`}}></span>
                        <strong>{link.label}</strong>
                    </Link>
                ))}
            </div>
        </section>

        <section className={styles.statsGrid}>
            <StatCard icon="/icons/account.svg" label="Účastníků v systému" value={totalAccounts} detail={`${dashboard?.staffCount ?? 0} organizátorů a učitelů`} />
            <StatCard icon="/icons/calc.svg" label="Rezervací povoleno" value={reservationsEnabled} detail={`${reservationRatio}% účtů má povolené rezervace`} />
            <StatCard icon="/icons/successmark.svg" label="Aktivní dnes" value={dashboard?.activeToday ?? 0} detail={`${dashboard?.activeNow ?? 0} aktivních za posledních 15 minut`} />
        </section>

        <section className={styles.insights}>
            <div className={styles.panel}>
                <div className={styles.panelHeader}>
                    <div>
                        <h2>Stav LAN Party</h2>
                        <p>Rychlý přehled aktivity a rezervací</p>
                    </div>
                    {/*<span>{activeRatio}%</span>*/}
                </div>
                <div className={styles.rings}>
                    <ProgressRing label="Rezervace" value={reservationRatio} />
                </div>
            </div>

            <div className={styles.panel}>
                <div className={styles.panelHeader}>
                    <div>
                        <h2>Třídy</h2>
                        <p>Největší skupiny s povolenou rezervací</p>
                    </div>
                </div>
                <div className={styles.bars}>
                    {(dashboard?.classBreakdown.length ? dashboard.classBreakdown : [{class: "Bez dat", count: 0}]).map(item => (
                        <div key={item.class} className={styles.barRow}>
                            <span>{item.class}</span>
                            <div>
                                <span style={{width: `${maxClassCount > 0 ? (item.count / maxClassCount) * 100 : 0}%`}}></span>
                            </div>
                            <strong>{item.count}</strong>
                        </div>
                    ))}
                </div>
            </div>

            <div className={styles.panel}>
                <div className={styles.panelHeader}>
                    <div>
                        <h2>Nové účty</h2>
                        <p>{account ? "Poslední registrace v systému" : "Soukromé údaje jsou dostupné po přihlášení"}</p>
                    </div>
                </div>
                {account ? (
                    <div className={styles.recent}>
                        {(dashboard?.latestAccounts.length ? dashboard.latestAccounts : []).map(item => (
                            <div key={`${item.fullName}-${item.createdAtUtc.toISOString()}`}>
                                <Avatar name={item.fullName} size="32px" src={item.avatarUrl} className={styles.avatar} />
                                
                                <div>
                                    <strong>{item.fullName}</strong>
                                    <p>{item.class ?? "Bez třídy"} · {item.createdAtUtc.toLocaleDateString("cs-CZ")}</p>
                                </div>
                            </div>
                        ))}
                        {!dashboard?.latestAccounts.length && <p className={styles.empty}>Zatím nejsou k dispozici žádné účty.</p>}
                    </div>
                ) : (
                    <div className={styles.privatePanel}>
                        <span style={{maskImage: "url(/icons/account.svg)"}}></span>
                        <strong>Jména jsou skrytá</strong>
                        <p>Přihlas se a uvidíš i poslední nové účty v systému.</p>
                        <Link href="/app/login">Přihlásit se</Link>
                    </div>
                )}
            </div>
        </section>
    </main>;
}

function StatCard({icon, label, value, detail}: {icon: string; label: string; value: number; detail: string}) {
    return <div className={styles.statCard}>
        <span style={{maskImage: `url(${icon})`}}></span>
        <div>
            <p>{label}</p>
            <strong>{value}</strong>
            <small>{detail}</small>
        </div>
    </div>;
}

function ProgressRing({label, value}: {label: string; value: number}) {
    return <div className={styles.ring} style={{"--progress": `${value}%`} as CSSProperties}>
        <div>
            <strong>{value}%</strong>
            <span>{label}</span>
        </div>
    </div>;
}

function ActivitySummary({activeNow, activeToday, staffCount}: {activeNow: number; activeToday: number; staffCount: number}) {
    return <div className={styles.activitySummary}>
        <InfoItem icon="/icons/successmark.svg" label="Online teď" value={activeNow} />
        <InfoItem icon="/icons/statistics.svg" label="Aktivní dnes" value={activeToday} />
        <InfoItem icon="/icons/user_with_shield.svg" label="Organizační tým" value={staffCount} />
    </div>;
}

function InfoItem({icon, label, value}: {icon: string; label: string; value: number}) {
    return <div className={styles.infoItem}>
        <span style={{maskImage: `url(${icon})`}}></span>
        <div>
            <strong>{value}</strong>
            <p>{label}</p>
        </div>
    </div>;
}
