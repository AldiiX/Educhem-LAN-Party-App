import {Computer, Reservation, Room} from "@/schemas/ReservationSchema";
import {useEffect, useRef} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";


export function useReservationsDisplay(rooms: Room[], computers: Computer[], reservations: Reservation[] | null, tab: string) {
    const { account } = useAuth();

    function clearReservationsDisplay() {
        const mapElement = document.querySelector(".map-reservation-area-main");
        if(!mapElement) return;

        computers.forEach(computer => {
            mapElement.querySelectorAll(".pc").forEach(pcElement => {
                pcElement.classList.remove("unavailable", "available", "taken-by-you");
            })
        })
    }

    useEffect(() => {
        if(rooms.length === 0 || computers.length === 0 || reservations == null) return;
        const mapElement = document.querySelector(".map-reservation-area-main");
        if(!mapElement) return;

        // pocitace
        computers.forEach(computer => {
            const pcElement = document.querySelector(`.pc#${computer.id}`);
            if(!pcElement) return;

            pcElement.classList.remove("unavailable", "available", "taken-by-you");
            const reservation = reservations.find(reservation => reservation.computer?.id === computer.id);

            // nastaveni barvicek
            if(!reservation) pcElement.classList.add("available")
            else if(typeof(reservation.profile) === "string") pcElement.classList.add("unavailable");
            else if(reservation.profile.id === account?.id) pcElement.classList.add("taken-by-you");
            else pcElement.classList.add("unavailable");
        })

        // mistnosti
        rooms.forEach(room => {
            const roomElement = mapElement.querySelector(`#ROOM_${room.id}`);
            if(!roomElement || !reservations) return;
            // console.log(roomElement);

            roomElement.classList.remove("available", "available", "taken-by-you");

            // promenny
            const hasAtLeastOneSpace = reservations.filter(r=>r.room?.id === room.id).length < room.capacity;
            const isTakenByYou = reservations.find(reservation => reservation.room?.id === room.id && typeof(reservation.profile) !== "string" && reservation.profile.id === account?.id);

            // nasteveni barvicek
            if(!reservations.find(reservation => reservation.room?.id === room.id))
                roomElement.classList.add("available");
            else if(isTakenByYou) roomElement.classList.add("taken-by-you");
            else if(hasAtLeastOneSpace) roomElement.classList.add("available");
            else roomElement.classList.add("unavailable");
        })

        //console.log(mapElement);
    }, [rooms,computers, tab, reservations, account]);

    return {
        clearReservationsDisplay,
    }
}