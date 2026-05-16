import { z } from "zod";

export const AchievementSchema = z.object({
    id: z.uuid(),
    key: z.string(),
    name: z.string(),
    description: z.string().nullish(),
    iconUrl: z.string().nullish(),
    isHidden: z.boolean().nullish(),
});

export const BadgeSchema = z.object({
    id: z.uuid(),
    name: z.string(),
    description: z.string().nullish(),
    iconUrl: z.string().nullish(),
});

export const AccountAchievementSchema = z.object({
    id: z.uuid(),
    achievement: AchievementSchema,
    isHidden: z.boolean().nullish(),
    createdAtUtc: z.coerce.date(),
});

export const AccountBadgeSchema = z.object({
    id: z.uuid(),
    badge: BadgeSchema,
    isTakenOut: z.boolean().nullish(),
});

export type Achievement = z.infer<typeof AchievementSchema>;
export type Badge = z.infer<typeof BadgeSchema>;
export type AccountAchievement = z.infer<typeof AccountAchievementSchema>;
export type AccountBadge = z.infer<typeof AccountBadgeSchema>;
