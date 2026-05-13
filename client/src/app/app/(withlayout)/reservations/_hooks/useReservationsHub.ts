"use client"

import {useCallback, useMemo, useState} from "react";
import {useSignalRHub} from "@/hooks/useSignalRHub";
import {BACKEND_URL} from "@/lib/vars";
import {Reservation, ReservationSchema} from "@/schemas/ReservationSchema";

export function useReservationsHub() {
    const [connectedIds, setConnectedIds] = useState<number | null>(null);
    const [reservations, setReservations] = useState<Reservation[] | null>(null);

    const handlers = useMemo(() => {
        return {
            ReceiveReservations: (payload: any) => {
                const data = ReservationSchema.array().parse(payload.reservations);
                setReservations(data);
            },

            ReceiveStatus: (payload: any) => {
                setConnectedIds(payload.connectedIds);
            }
        };
    }, []);

    const hub = useSignalRHub("/hubs/reservations", {
        // baseUrl: BACKEND_URL,
        handlers,
        logLevel: process.env.NODE_ENV === "development"
            ? 2
            : 3,
        reconnectDelays: [0, 2000, 10000],
        startRetryDelays: [0, 1000, 3000]
    });

    const sendMessage = useCallback(async (message: string) => {
        await hub.invoke("SendMessage", message);
    }, [hub]);

    return {
        ...hub,
        reservations,
        connectedIds,
        sendMessage
    };
}