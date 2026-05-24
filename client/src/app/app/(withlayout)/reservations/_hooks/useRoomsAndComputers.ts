import useSWR from "swr";
import {Computer, ComputerSchema, Room, RoomSchema} from "@/schemas/ReservationSchema";
import { z } from "zod";
import {fetcher} from "@/lib/swr";
import { useMemo } from "react";

const FetchResponseSchema = z.object({
    rooms: RoomSchema.array(),
    computers: ComputerSchema.array(),
})

type FetchResponse = z.infer<typeof FetchResponseSchema>;

const racFetcher = async (url: string) => {
    const response = await fetcher<unknown>(url);

    return FetchResponseSchema.parse(response ?? []);
};

export function useRoomsAndComputers() {
    const {data, error, isLoading, mutate} = useSWR<FetchResponse>("/api/v1/reservations/rooms-and-computers", racFetcher);
    const rooms = data?.rooms ?? [];
    const computers = data?.computers ?? [];

    const capacities = useMemo(() => {
        const roomsCapacity = rooms.reduce((capacity, room) => capacity + room.capacity, 0);
        const computersCapacity = computers.length;

        return {
            roomsCapacity,
            computersCapacity,
            maxCapacity: roomsCapacity + computersCapacity,
        };
    }, [rooms, computers]);

    const refreshRoomsAndComputers = async () => await mutate();

    return {
        computers,
        rooms,
        maxCapacity: capacities.maxCapacity,
        roomsCapacity: capacities.roomsCapacity,
        computersCapacity: capacities.computersCapacity,
        racFetchError: error,
        racFetchLoading: isLoading,
        refreshRoomsAndComputers,
    };
}
