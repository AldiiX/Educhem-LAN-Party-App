import {z} from "zod";

export const LogEntrySchema = z.object({
    id: z.union([z.number(), z.string()]),
    type: z.string(),
    exactType: z.string(),
    message: z.string(),
    date: z.coerce.date().nullable(),
});

export type LogEntry = z.infer<typeof LogEntrySchema>;
