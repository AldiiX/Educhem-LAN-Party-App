import {Computer, Reservation, Room} from "@/schemas/ReservationSchema";
import {useMemo} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";

type ReservationDisplayClass = "available" | "unavailable" | "taken-by-you";

export function useReservationsDisplay(rooms: Room[], computers: Computer[], reservations: Reservation[] | null) {
    const { account } = useAuth();

    const computerClassById = useMemo(() => {
        const result = new Map<string, ReservationDisplayClass>();
        if(reservations == null) return result;

        computers.forEach(computer => {
            const reservation = reservations.find(reservation => reservation.computer?.id === computer.id);

            // nasteveni class (=barvicek)
            if(!reservation) result.set(computer.id, "available");
            else if(typeof(reservation.profile) !== "string" && reservation.profile.id === account?.id) result.set(computer.id, "taken-by-you");
            else result.set(computer.id, "unavailable");
        });

        return result;
    }, [computers, reservations, account?.id]);

    const roomClassById = useMemo(() => {
        const result = new Map<string, ReservationDisplayClass>();
        if(reservations == null) return result;

        rooms.forEach(room => {
            const roomReservations = reservations.filter(reservation => reservation.room?.id === room.id);
            const isTakenByYou = roomReservations.some(reservation => (
                typeof(reservation.profile) !== "string" && reservation.profile.id === account?.id
            ));

            // nastaveni class (=barvicek)
            if(isTakenByYou) result.set(room.id, "taken-by-you");
            else if(roomReservations.length < room.capacity) result.set(room.id, "available");
            else result.set(room.id, "unavailable");
        });

        return result;
    }, [rooms, reservations, account?.id]);

    return {
        getComputerClass: (id: string) => computerClassById.get(id) ?? "",
        getRoomClass: (id: string) => roomClassById.get(id) ?? "",
    }
}
