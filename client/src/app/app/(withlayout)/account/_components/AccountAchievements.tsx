import styles from "./AccountAchievements.module.scss";
import { Account } from "@/schemas/AccountSchema";
import { AccountPageState } from "../_hooks/types";
import { BadgeCard } from "./BadgeCard";

type AccountAchievementsProps = {
    account: Account;
    state: AccountPageState;
};

export function AccountAchievements({ account, state }: AccountAchievementsProps) {
    const { achievementUpdatingIds, badgeUpdatingIds } = state;
    const achievements = account.achievements ?? [];
    const badges = account.badges ?? [];
    const takenOutCount = badges.filter((entry) => entry.isTakenOut).length;

    return <section className={styles.wrapper}>
        <div className={styles.block}>
            <h2>Achievementy</h2>
            {achievements.length === 0 ? (
                <p className={styles.empty}>Zatím žádné achievementy.</p>
            ) : (
                <div className={styles.achievementList}>
                    {achievements.map((entry) => (
                        <article key={entry.id} className={styles.achievementRow}>
                            <div className={styles.achievementMain}>
                                <div className={styles.achievementIcon}>
                                    {entry.achievement.iconUrl ? (
                                        <img src={entry.achievement.iconUrl} alt="" />
                                    ) : (
                                        <span>{entry.achievement.name.charAt(0)}</span>
                                    )}
                                </div>
                                <div className={styles.achievementInfo}>
                                    <h3>{entry.achievement.name}</h3>
                                    {entry.achievement.description && <p>{entry.achievement.description}</p>}
                                </div>
                                <div className={styles.achievementMeta}>
                                    <span>Získáno {entry.createdAtUtc.toLocaleDateString("cs-CZ")}</span>
                                </div>
                            </div>
                            <div className={styles.achievementActions}>
                                <button
                                    type="button"
                                    className={styles.actionButton}
                                    disabled={achievementUpdatingIds.has(entry.id)}
                                    onClick={() => state.toggleAchievementVisibility(entry.id, !entry.isHidden)}
                                >
                                    {entry.isHidden ? "Zobrazit" : "Skrýt"}
                                </button>
                            </div>
                        </article>
                    ))}
                </div>
            )}
        </div>

        <div className={styles.block}>
            <h2>Badge</h2>
            {badges.length === 0 ? (
                <p className={styles.empty}>Zatím žádné badge.</p>
            ) : (
                <div className={styles.grid}>
                    {badges.map((entry) => (
                        <div key={entry.id} className={styles.badgeItem}>
                            <BadgeCard entry={entry} variant="full" />
                            <div className={styles.badgeActions}>
                                <button
                                    type="button"
                                    className={styles.actionButton}
                                    disabled={badgeUpdatingIds.has(entry.id) || (!entry.isTakenOut && takenOutCount >= 3)}
                                    onClick={() => state.toggleBadgeTakenOut(entry.id, !entry.isTakenOut)}
                                >
                                    {entry.isTakenOut ? "Sundat" : "Vzít na profil"}
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    </section>;
}
