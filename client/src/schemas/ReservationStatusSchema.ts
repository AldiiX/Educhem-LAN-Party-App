import {z} from "zod";

export const ReservationStatusSchema = z.object({
    maxCapacity: z.number(),
    capacityUsed: z.number(),
    capacityUsedPercentage: z.number(),
    accountsWithEnabledReservations: z.number(),
    accountsWithEnabledReservationsPercentage: z.number(),
    reservationsEnabled: z.boolean(),
    reservationsStatus: z.enum(["UseTimer", "Open", "Closed"]),
    serverNow: z.coerce.date(),
    reservationsEnabledFrom: z.coerce.date(),
    reservationsEnabledTo: z.coerce.date(),
    message: z.string(),
});

export type ReservationStatus = z.infer<typeof ReservationStatusSchema>;
