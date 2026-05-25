import {z} from "zod";
import {AccountSchema} from "@/schemas/AccountSchema";

export const AttendanceEntryTypeSchema = z.enum(["CheckIn", "CheckOut"]);

export const AttendanceEntrySchema = z.object({
    id: z.uuid(),
    type: AttendanceEntryTypeSchema,
    reason: z.string().nullish(),
    profile: AccountSchema,
    createdBy: AccountSchema,
    createdAtUtc: z.coerce.date(),
    updatedAtUtc: z.coerce.date(),
});

export const AttendanceParticipantSchema = z.object({
    profile: AccountSchema,
    currentState: AttendanceEntryTypeSchema.nullish(),
    latestEntry: AttendanceEntrySchema.nullish(),
});

export const AttendanceOverviewSchema = z.object({
    entries: z.array(AttendanceEntrySchema),
    participants: z.array(AttendanceParticipantSchema),
    stats: z.object({
        present: z.number(),
        away: z.number(),
        total: z.number(),
    }),
    attendanceEnabled: z.boolean(),
});

export const AttendanceDeltaSchema = z.object({
    entry: AttendanceEntrySchema,
    participant: AttendanceParticipantSchema,
    stats: z.object({
        present: z.number(),
        away: z.number(),
        total: z.number(),
    }),
});

export type AttendanceEntryType = z.infer<typeof AttendanceEntryTypeSchema>;
export type AttendanceEntry = z.infer<typeof AttendanceEntrySchema>;
export type AttendanceParticipant = z.infer<typeof AttendanceParticipantSchema>;
export type AttendanceOverview = z.infer<typeof AttendanceOverviewSchema>;
export type AttendanceDelta = z.infer<typeof AttendanceDeltaSchema>;
