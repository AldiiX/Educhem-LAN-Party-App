import { z } from "zod";
import { AccountAchievementSchema, AccountBadgeSchema } from "@/schemas/AchievementBadgeSchema";

export const AccountGenderSchema = z.enum(["Male", "Female", "Other"]);

export const AccountTypeSchema = z.enum(["Student", "Teacher", "TeacherOrg", "Admin", "SuperAdmin"]);

export const AccountCommunicationStyleSchema = z.enum(["Informal", "Formal"]);

export const AvatarSyncPlatformSchema = z.enum(["Discord", "GitHub", "Google", "Instagram"]);

export const SchoolSchema = z.object({
    id: z.uint32(),
    slug: z.string(),
    shortName: z.string(),
    displayName: z.string(),
    iconUrl: z.string().nullish(),
});

export const EnrollmentSchema = z.object({
    school: SchoolSchema,
    class: z.string().nullish(),
});

export const AccountSchema = z.object({
    id: z.uuid(),
    firstName: z.string(),
    lastName: z.string(),
    fullName: z.string(),
    email: z.string().nullish(),
    avatarUrl: z.string().nullish(),
    bannerUrl: z.string().nullish(),
    discordUsername: z.string().nullish(),
    avatarSyncPlatform: AvatarSyncPlatformSchema.nullish(),
    accountType: AccountTypeSchema.nullish(),
    createdAtUtc: z.coerce.date(),
    updatedAtUtc: z.coerce.date().nullish(),
    lastActiveUtc: z.coerce.date().nullish(),
    gender: AccountGenderSchema.nullish(),
    communicationStyle: AccountCommunicationStyleSchema.default("Formal"),
    enrollment: EnrollmentSchema.nullish(),
    enableReservations: z.boolean().nullish(),
    achievements: z.array(AccountAchievementSchema).optional().default([]),
    badges: z.array(AccountBadgeSchema).optional().default([]),
});

export type Account = z.infer<typeof AccountSchema>;
export type AccountGender = z.infer<typeof AccountGenderSchema>;
export type AccountType = z.infer<typeof AccountTypeSchema>;
export type AccountCommunicationStyle = z.infer<typeof AccountCommunicationStyleSchema>;
export type AvatarSyncPlatform = z.infer<typeof AvatarSyncPlatformSchema>;
