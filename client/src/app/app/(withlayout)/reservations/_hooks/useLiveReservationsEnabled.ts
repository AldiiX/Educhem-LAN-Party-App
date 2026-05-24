import {useEffect, useMemo, useState} from "react";
import {ReservationStatus} from "@/schemas/ReservationStatusSchema";
import {toTimestamp} from "@/app/app/(withlayout)/reservations/_hooks/time";

export function useLiveReservationsEnabled(
    status: ReservationStatus | null,
    onBoundaryReached: () => void,
) {
    const serverOffset = useMemo(() => (
        status ? toTimestamp(status.serverNow) - Date.now() : 0
    ), [status]);
    const [now, setNow] = useState(() => Date.now() + serverOffset);

    useEffect(() => {
        if (!status) {
            return;
        }

        setNow(Date.now() + serverOffset);

        const interval = window.setInterval(() => {
            setNow(Date.now() + serverOffset);
        }, 1000);

        return () => window.clearInterval(interval);
    }, [serverOffset, status]);

    useEffect(() => {
        if (!status || status.reservationsStatus !== "UseTimer") {
            return;
        }

        const serverNow = toTimestamp(status.serverNow);
        const from = toTimestamp(status.reservationsEnabledFrom);
        const to = toTimestamp(status.reservationsEnabledTo);

        if (!Number.isFinite(serverNow) || !Number.isFinite(from) || !Number.isFinite(to)) {
            return;
        }

        const target = serverNow < from
            ? from
            : serverNow < to
                ? to
                : null;

        if (target === null) {
            return;
        }

        const diff = target - serverNow;
        const maxTimeout = 2147483647;

        if (diff <= 0 || diff > maxTimeout) {
            return;
        }

        const timeout = window.setTimeout(onBoundaryReached, diff + 500);

        return () => window.clearTimeout(timeout);
    }, [onBoundaryReached, status]);

    return useMemo(() => {
        if (!status) {
            return false;
        }

        if (status.reservationsStatus === "Open") {
            return true;
        }

        if (status.reservationsStatus === "Closed") {
            return false;
        }

        const from = toTimestamp(status.reservationsEnabledFrom);
        const to = toTimestamp(status.reservationsEnabledTo);

        if (!Number.isFinite(now) || !Number.isFinite(from) || !Number.isFinite(to)) {
            return status.reservationsEnabled;
        }

        return now >= from && now <= to;
    }, [now, status]);
}
