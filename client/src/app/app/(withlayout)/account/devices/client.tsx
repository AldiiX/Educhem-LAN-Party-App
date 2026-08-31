"use client";

import styles from "./client.module.scss";
import {useAccountSessions} from "../_hooks/useAccountSessions";
import {Button} from "@/components/Button";
import {AuthSession} from "@/schemas/AuthSessionSchema";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {phrase} from "@/lib/communicationStyle";

function DeviceIcon({type}: {type?: string | null}) {
    if (type === "Mobile") {
        return <span className={styles.iconMask} style={{maskImage: "url(/icons/phone.svg)"}} />;
    }

    if (type === "Tablet") {
        return <span className={styles.iconMask} style={{maskImage: "url(/icons/phone.svg)"}} />;
    }

    return <span className={styles.iconMask} style={{maskImage: "url(/icons/computer.svg)"}} />;
}

function formatRelativeTime(date: Date): string {
    const time = date.getTime();
    if (isNaN(time) || date.getFullYear() < 2000) return "Neznámá";

    const diffMs = Date.now() - time;
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHours = Math.floor(diffMin / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffSec < 60) return "Právě teď";
    if (diffMin < 60) return `Před ${diffMin} ${diffMin === 1 ? "minutou" : diffMin < 5 ? "minutami" : "minutami"}`;
    if (diffHours < 24) return `Před ${diffHours} ${diffHours === 1 ? "hodinou" : diffHours < 5 ? "hodinami" : "hodinami"}`;
    if (diffDays < 7) return `Před ${diffDays} ${diffDays === 1 ? "dnem" : diffDays < 5 ? "dny" : "dny"}`;
    return date.toLocaleDateString("cs-CZ");
}

export default function AccountDevices() {
    const {account} = useAuth();
    const {
        sessions,
        isLoading,
        error,
        revokingId,
        revokingOthers,
        revokeSession,
        revokeOtherSessions,
        refreshSessions,
    } = useAccountSessions();

    const otherSessionsCount = sessions.filter(s => !s.isCurrent).length;

    if (error) {
        return <section className={styles.devicesSection}>
            <div className={styles.errorState}>
                <p>Nepodařilo se načíst seznam přihlášených zařízení.</p>
                <Button type="secondary" text="Zkusit znovu" icon="/icons/reload.svg" onClick={() => refreshSessions()} />
            </div>
        </section>;
    }

    return <section className={styles.devicesSection}>
        <div className={styles.header}>
            <div className={styles.headerText}>
                <h2>Přihlášená zařízení</h2>
                <p>{phrase(
                    account?.communicationStyle,
                    "Spravuj zařízení a relace, které mají přístup k tvému účtu. Pokud poznáš neznámé zařízení, okamžitě ho odhlaš.",
                    "Spravujte zařízení a relace, které mají přístup k vašemu účtu. Pokud poznáte neznámé zařízení, okamžitě ho odhlaste."
                )}</p>
            </div>
            {otherSessionsCount > 0 && (
                <Button
                    type="secondary"
                    text="Odhlásit ostatní zařízení"
                    icon="/icons/logout.svg"
                    onClick={revokeOtherSessions}
                    loading={revokingOthers}
                    disabled={revokingOthers || isLoading}
                    className={styles.revokeAllBtn}
                />
            )}
        </div>

        {isLoading && sessions.length === 0 ? (
            <div className={styles.loadingState}>
                <div className={styles.spinner}></div>
                <p>Načítám přihlášená zařízení...</p>
            </div>
        ) : (
            <div className={styles.devicesList}>
                {sessions.map((session: AuthSession) => {
                    const os = session.operatingSystem || "Neznámý operační systém";
                    const browser = session.browser || "Neznámý prohlížeč";
                    const location = [session.city, session.country].filter(Boolean).join(", ") || "Neznámá lokace";
                    const createdDate = new Date(session.createdAtUtc);
                    const lastActiveDate = new Date(session.lastActiveUtc);

                    return <div
                        key={session.id}
                        className={`${styles.deviceCard} ${session.isCurrent ? styles.currentCard : ""}`}
                    >
                        <div className={styles.deviceCardHeader}>
                            <div className={styles.deviceIconWrap}>
                                <DeviceIcon type={session.deviceType} />
                            </div>
                            <div className={styles.deviceMainInfo}>
                                <div className={styles.deviceTitleRow}>
                                    <h3 className={styles.deviceName}>{os} · {browser}</h3>
                                    {session.isCurrent && (
                                        <span className={styles.currentBadge}>
                                            <span className={styles.pulseDot} />
                                            Toto zařízení
                                        </span>
                                    )}
                                </div>
                                <p className={styles.deviceTypeLabel}>{session.deviceType || "Počítač"}</p>
                            </div>
                        </div>

                        <div className={styles.deviceMetaList}>
                            <div className={styles.metaRow}>
                                <span className={styles.rowLabel}>
                                    <span className={styles.rowIcon} style={{maskImage: "url(/icons/info.svg)"}} />
                                    IP adresa
                                </span>
                                <span className={styles.rowValue}>
                                    <code>{session.ipAddress || "Neznámá"}</code>
                                </span>
                            </div>

                            <div className={styles.metaRow}>
                                <span className={styles.rowLabel}>
                                    <span className={styles.rowIcon} style={{maskImage: "url(/icons/map.svg)"}} />
                                    Lokace
                                </span>
                                <span className={styles.rowValue}>{location}</span>
                            </div>

                            <div className={styles.metaRow}>
                                <span className={styles.rowLabel}>
                                    <span className={styles.rowIcon} style={{maskImage: "url(/icons/calendar.svg)"}} />
                                    Přihlášeno
                                </span>
                                <span className={styles.rowValue}>
                                    {createdDate.getFullYear() > 2000 ? createdDate.toLocaleString("cs-CZ") : "Neznámé datum"}
                                </span>
                            </div>

                            <div className={styles.metaRow}>
                                <span className={styles.rowLabel}>
                                    <span className={styles.rowIcon} style={{maskImage: "url(/icons/reload.svg)"}} />
                                    Poslední aktivita
                                </span>
                                <span className={styles.rowValue}>
                                    {formatRelativeTime(lastActiveDate)}
                                </span>
                            </div>
                        </div>

                        {!session.isCurrent && (
                            <div className={styles.deviceCardFooter}>
                                <Button
                                    type="secondary"
                                    text="Odhlásit zařízení"
                                    icon="/icons/logout.svg"
                                    onClick={() => revokeSession(session.id)}
                                    loading={revokingId === session.id}
                                    disabled={revokingId === session.id || revokingOthers}
                                    className={styles.revokeSingleBtn}
                                />
                            </div>
                        )}
                    </div>;
                })}
            </div>
        )}
    </section>;
}
