import {useMemo} from "react";
import {Reservation} from "@/schemas/ReservationSchema";

export function useReservationStats(
    reservations: Reservation[] | null,
    computersCapacity: number,
    roomsCapacity: number,
    maxCapacity: number,
) {
    return useMemo(() => {
        const reservedComputers = reservations?.filter(reservation => reservation.computer !== null).length ?? 0;
        const reservedRooms = reservations?.filter(reservation => reservation.room !== null).length ?? 0;
        const reservedCount = reservedComputers + reservedRooms;

        return {
            reservedComputers,
            reservedRooms,
            reservedCount,
            computersCapacity,
            roomsCapacity,
            maxCapacity,
            filledCapacityPercentage: Math.min(100, Math.round((reservedCount / Math.max(maxCapacity, 1)) * 100)),
        };
    }, [computersCapacity, maxCapacity, reservations, roomsCapacity]);
}
