import { z } from "zod";

export const AccountGenderSchema = z.enum(["Male", "Female", "Other"]);

export const AccountTypeSchema = z.enum(["Student", "Teacher", "TeacherOrg", "Admin", "SuperAdmin"]);

export const AccountSchema = z.object({
    id: z.uuid(),
    firstName: z.string(),
    lastName: z.string(),
    fullName: z.string(),
    email: z.string().nullish(),
    avatarUrl: z.string().nullish(),
    bannerUrl: z.string().nullish(),
    accountType: AccountTypeSchema.nullish(),
    createdAtUtc: z.coerce.date().nullish(),
    updatedAtUtc: z.coerce.date().nullish(),
    lastActiveUtc: z.coerce.date().nullish(),
    gender: AccountGenderSchema.nullish(),
    school: z.object({
        id: z.uint32(),
        slug: z.string(),
        shortName: z.string(),
        displayName: z.string(),
        iconUrl: z.string().nullish(),
    }).nullish(),
    class: z.string().nullish(),
    enableReservations: z.boolean().nullish(),
});

export type Account = z.infer<typeof AccountSchema>;
export type AccountGender = z.infer<typeof AccountGenderSchema>;
export type AccountType = z.infer<typeof AccountTypeSchema>;