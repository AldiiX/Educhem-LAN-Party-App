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

    const maxCapacity = useMemo(() => {
        let c = 0;
        if(computers.length === 0 || rooms.length === 0) return c;

        computers.forEach(_ => {c++});
        rooms.forEach(room => {
            c = c + room.capacity;
        });

        return c;
    }, [rooms,computers])

    const roomsCapacity = useMemo(() => {
        let c = 0;
        if(rooms.length === 0) return c;

        rooms.forEach(room => {
            c = c + room.capacity;
        });

        return c;
    }, [rooms])

    const computersCapacity = useMemo(() => {
        if(computers.length === 0) return 0;
        return computers.length;
    }, [computers])

    const refreshRoomsAndComputers = async () => await mutate();

    return {
        computers,
        rooms,
        maxCapacity,
        roomsCapacity,
        computersCapacity,
        racFetchError: error,
        racFetchLoading: isLoading,
        refreshRoomsAndComputers,
    };
}
