import { z } from 'zod'
import {AccountSchema} from "@/schemas/AccountSchema";

export const RoomSchema = z.object({
    label: z.string(),
    imageUrl: z.string().nullable(),
    capacity: z.number(),
    available: z.boolean(),
    id: z.string(),
});

export const ComputerSchema = z.object({
    imageUrl: z.string().nullable(),
    room: RoomSchema.nullable(),
    available: z.boolean(),
    isTeachersComputer: z.boolean(),
    label: z.string(),
    id: z.string(),
})

export const ReservationSchema = z.object({
    id: z.string(),
    profile: z.union([AccountSchema, z.literal("Anonymous")]),
    note: z.string().nullable(),
    updatedAtUtc: z.coerce.date(),
    createdAtUtc: z.coerce.date(),
    room: RoomSchema.nullable(),
    computer: ComputerSchema.nullable(),
})


export type Reservation = z.infer<typeof ReservationSchema>
export type Room = z.infer<typeof RoomSchema>
export type Computer = z.infer<typeof ComputerSchema>
