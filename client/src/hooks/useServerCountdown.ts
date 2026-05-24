"use client";

import {useEffect, useMemo, useState} from "react";

export function formatCountdownDuration(milliseconds: number) {
    const totalSeconds = Math.max(0, Math.floor(milliseconds / 1000));
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    return [
        days > 0 ? `${days}d` : null,
        `${hours}h`,
        `${minutes}min`,
        `${seconds}s`,
    ].filter(Boolean).join(" ");
}

export function useServerCountdown(serverNow: Date, target: Date) {
    const serverOffset = useMemo(() => {
        return serverNow.getTime() - Date.now();
    }, [serverNow]);

    const [now, setNow] = useState(() => Date.now() + serverOffset);

    useEffect(() => {
        setNow(Date.now() + serverOffset);

        const interval = window.setInterval(() => {
            setNow(Date.now() + serverOffset);
        }, 1000);

        return () => window.clearInterval(interval);
    }, [serverOffset]);

    const targetMs = target.getTime();
    const diffMs = targetMs - now;
    const isValid = Number.isFinite(serverOffset) && Number.isFinite(targetMs);

    return {
        diffMs,
        formatted: formatCountdownDuration(diffMs),
        isFinished: diffMs <= 0,
        isValid,
        now,
        serverOffset,
        targetMs,
    };
}
