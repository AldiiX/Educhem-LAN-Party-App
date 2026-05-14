import styles from "./AccountAchievements.module.scss";
import { AccountBadge } from "@/schemas/AchievementBadgeSchema";

type BadgeCardProps = {
    entry: AccountBadge;
    variant?: "full" | "compact";
};

export function BadgeCard({ entry, variant = "full" }: BadgeCardProps) {
    return (
        <article className={variant === "full" ? styles.badgeFull : styles.badgeCompact}>
            {entry.badge.iconUrl ? (
                <img className={styles.icon} src={entry.badge.iconUrl} alt="" />
            ) : (
                <div className={styles.iconPlaceholder}>{entry.badge.name.charAt(0)}</div>
            )}
            {variant === "full" && (
                <div className={styles.cardContent}>
                    <h3>{entry.badge.name}</h3>
                    {entry.badge.description && <p>{entry.badge.description}</p>}
                </div>
            )}
        </article>
    );
}
