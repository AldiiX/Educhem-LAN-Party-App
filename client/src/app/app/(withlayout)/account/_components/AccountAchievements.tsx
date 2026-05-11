import styles from "./AccountAchievements.module.scss";
import { Account } from "@/schemas/AccountSchema";
import { AccountPageState } from "../_hooks/types";

type AccountAchievementsProps = {
    account: Account;
    state: AccountPageState;
};

export function AccountAchievements({ account, state }: AccountAchievementsProps) {
    const { achievementUpdatingIds, badgeUpdatingIds } = state;
    const achievements = account.achievements ?? [];
    const badges = account.badges ?? [];

    return <section className={styles.wrapper}>
        <div className={styles.block}>
            <h2>Achievementy</h2>
            {achievements.length === 0 ? (
                <p className={styles.empty}>Zatím žádné achievementy.</p>
            ) : (
                <div className={styles.achievementList}>
                    {achievements.map((entry) => (
                        <article key={entry.id} className={styles.achievementCard}>
                            <div
                                className={styles.achievementBanner}
                                style={entry.achievement.iconUrl ? { backgroundImage: `url(${entry.achievement.iconUrl})` } : undefined}
                            >
                                {!entry.achievement.iconUrl && <span>{entry.achievement.name.charAt(0)}</span>}
                            </div>
                            <div className={styles.achievementBody}>
                                <div className={styles.achievementHeader}>
                                    <h3>{entry.achievement.name}</h3>
                                    <span>{entry.createdAtUtc.toLocaleDateString("cs-CZ")}</span>
                                </div>
                                {entry.achievement.description && <p>{entry.achievement.description}</p>}
                                <div className={styles.cardActions}>
                                    <button
                                        type="button"
                                        className={styles.actionButton}
                                        disabled={achievementUpdatingIds.has(entry.id)}
                                        onClick={() => state.toggleAchievementVisibility(entry.id, !entry.isHidden)}
                                    >
                                        {entry.isHidden ? "Zobrazit" : "Skrýt"}
                                    </button>
                                </div>
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
                        <article key={entry.id} className={styles.card}>
                            {entry.badge.iconUrl ? (
                                <img className={styles.icon} src={entry.badge.iconUrl} alt="" />
                            ) : (
                                <div className={styles.iconPlaceholder}>{entry.badge.name.charAt(0)}</div>
                            )}
                            <div className={styles.cardContent}>
                                <h3>{entry.badge.name}</h3>
                                {entry.badge.description && <p>{entry.badge.description}</p>}
                                <div className={styles.cardActions}>
                                    <button
                                        type="button"
                                        className={styles.actionButton}
                                        disabled={badgeUpdatingIds.has(entry.id)}
                                        onClick={() => state.toggleBadgeTakenOut(entry.id, !entry.isTakenOut)}
                                    >
                                        {entry.isTakenOut ? "Sundat" : "Vzít na profil"}
                                    </button>
                                </div>
                            </div>
                        </article>
                    ))}
                </div>
            )}
        </div>
    </section>;
}
