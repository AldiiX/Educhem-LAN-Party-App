"use client"

import {useCallback, useMemo, useRef, useState} from "react";
import {useSignalRHub} from "@/hooks/useSignalRHub";
import {BACKEND_URL} from "@/lib/vars";
import {Reservation, ReservationSchema} from "@/schemas/ReservationSchema";
import toast from "react-hot-toast";

export function useReservationsHub() {
    const [connectedIds, setConnectedIds] = useState<number | null>(null);
    const [reservations, setReservations] = useState<Reservation[] | null>(null);
    const [isReservationMutationPending, setIsReservationMutationPending] = useState(false);
    // state se prepise az po renderu, ref je hned - takze dvojklik nemuze poslat druhej request
    const reservationMutationPendingRef = useRef(false);

    const setParsedReservations = useCallback((reservations: unknown) => {
        const data = ReservationSchema.array().parse(reservations);
        setReservations(data);
    }, []);

    const applyReservationChange = useCallback((payload: any) => {
        // server posila jen zmenu, ne celej seznam, tak si to tady slepi lokalne
        const previousReservation = payload.previousReservation
            ? ReservationSchema.parse(payload.previousReservation)
            : null;
        const reservation = payload.reservation
            ? ReservationSchema.parse(payload.reservation)
            : null;

        setReservations(currentReservations => {
            if(!currentReservations) return currentReservations;

            let nextReservations = previousReservation
                ? removeReservation(currentReservations, previousReservation)
                : currentReservations;

            if(reservation) {
                nextReservations = removeReservation(nextReservations, reservation);
                nextReservations = [reservation, ...nextReservations];
            }

            return nextReservations;
        });
    }, []);

    const handlers = useMemo(() => {
        return {
            ReceiveReservations: (payload: any) => {
                setParsedReservations(payload.reservations);
            },

            ReceiveStatus: (payload: any) => {
                setConnectedIds(payload.connectedIds);
            },

            ReceiveError: (payload: any) => {
                toast.error(getErrorMessage(payload));
            },

            ReservationsChanged: (payload: any) => {
                applyReservationChange(payload);
            }
        };
    }, [applyReservationChange, setParsedReservations]);

    const hub = useSignalRHub("/hubs/reservations", {
        // baseUrl: BACKEND_URL,
        handlers,
        logLevel: process.env.NODE_ENV === "development"
            ? 2
            : 3,
        reconnectDelays: [0, 2000, 10000],
        startRetryDelays: [0, 1000, 3000]
    });
    const { invoke } = hub;

    const sendMessage = useCallback(async (message: string) => {
        await invoke("SendMessage", message);
    }, [invoke]);

    const reserve = useCallback(async (id: string, type: "room" | "computer") => {
        if(reservationMutationPendingRef.current) return;

        reservationMutationPendingRef.current = true;
        setIsReservationMutationPending(true);
        try {
            await invoke("Reserve", {
                id,
                type: type,
            });
        } finally {
            reservationMutationPendingRef.current = false;
            setIsReservationMutationPending(false);
        }
    }, [invoke])

    const unbook = useCallback(async () => {
        if(reservationMutationPendingRef.current) return;

        reservationMutationPendingRef.current = true;
        setIsReservationMutationPending(true);
        try {
            await invoke("Unbook")
        } finally {
            reservationMutationPendingRef.current = false;
            setIsReservationMutationPending(false);
        }
    }, [invoke])

    return {
        ...hub,
        reservations,
        connectedIds,
        isReservationMutationPending,
        sendMessage,
        reserve,
        unbook
    };
}

function removeReservation(reservations: Reservation[], reservationToRemove: Reservation) {
    // rezervace uz ma svoje id, tak zadny bullshity podle roomky a casu
    const index = reservations.findIndex(reservation => reservation.id === reservationToRemove.id);
    if(index === -1) return reservations;

    return reservations.filter((_, reservationIndex) => reservationIndex !== index);
}

function getErrorMessage(payload: unknown) {
    if(typeof payload === "string") return payload;

    if(
        payload
        && typeof payload === "object"
        && "message" in payload
        && typeof payload.message === "string"
    ) {
        return payload.message;
    }

    return "Něco se nepodařilo. Zkuste to prosím znovu.";
}
