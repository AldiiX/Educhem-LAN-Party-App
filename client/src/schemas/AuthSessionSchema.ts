import {z} from "zod";

export const AuthSessionSchema = z.object({
    id: z.string().uuid(),
    deviceType: z.string().nullable().optional(),
    browser: z.string().nullable().optional(),
    operatingSystem: z.string().nullable().optional(),
    ipAddress: z.string().nullable().optional(),
    city: z.string().nullable().optional(),
    country: z.string().nullable().optional(),
    createdAtUtc: z.coerce.date(),
    lastActiveUtc: z.coerce.date(),
    isCurrent: z.boolean(),
});

export type AuthSession = z.infer<typeof AuthSessionSchema>;
