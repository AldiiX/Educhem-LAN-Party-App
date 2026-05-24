"use client";

import {ReservationStatus} from "@/schemas/ReservationStatusSchema";
import style from "./ReservationCountdownStatus.module.scss";
import {useEffect, useMemo, useState} from "react";
import {formatCountdownDuration} from "@/hooks/useServerCountdown";



export function ReservationCountdownStatus({
   status,
   reservationsEnabled,
    className = "",
                                           }: {
    status: ReservationStatus;
    reservationsEnabled: boolean;
    className?: string;
}) {
    const serverOffset = useMemo(() => status.serverNow.getTime() - Date.now(), [status.serverNow]);
    const [now, setNow] = useState(() => Date.now() + serverOffset);

    useEffect(() => {
        setNow(Date.now() + serverOffset);

        const interval = window.setInterval(() => {
            setNow(Date.now() + serverOffset);
        }, 1000);

        return () => window.clearInterval(interval);
    }, [serverOffset]);

    const fromMs = status.reservationsEnabledFrom.getTime();
    const toMs = status.reservationsEnabledTo.getTime();
    const timerUsable = Number.isFinite(serverOffset) && Number.isFinite(fromMs) && Number.isFinite(toMs);

    const timerState = useMemo(() => {
        if (! timerUsable) {
            return {label: "Stav rezervací", value: status.message, target: null};
        }

        if (status.reservationsStatus === "Open") {
            return {label: "Rezervace jsou otevřené", value: "", target: null};
        }

        if (status.reservationsStatus === "Closed") {
            return {label: "Rezervace jsou uzavřené", value: "", target: null};
        }

        if (now < fromMs) {
            return {label: "Rezervace se otevřou za", value: formatCountdownDuration(fromMs - now), target: fromMs};
        }

        if (now <= toMs) {
            return {label: "Rezervace se zavřou za", value: formatCountdownDuration(toMs - now), target: toMs};
        }

        return {label: "Rezervace jsou uzavřené", value: "", target: null};
    }, [fromMs, now, status.message, status.reservationsStatus, timerUsable, toMs]);

    return (
        <div className={`${style.reservationStatus} ${reservationsEnabled ? style.open : style.closed} ${className}`}>
            <p>{timerState.label}</p>
            <strong>{timerState.value}</strong>
        </div>
    );
}
