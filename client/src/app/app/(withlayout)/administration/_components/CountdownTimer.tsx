"use client";

import {useEffect, useMemo, useState} from "react";

type CountdownTimerProps = {
    serverNow: Date;
    target: Date;
    prefix?: string;
    finishedText?: string;
    onFinished?: () => void;
    className?: string;
};

export function CountdownTimer({
                                   serverNow,
                                   target,
                                   prefix = "",
                                   finishedText = "Čas vypršel",
                                   onFinished,
                                   className,
                               }: CountdownTimerProps) {
    const serverOffset = useMemo(() => {
        return serverNow.getTime() - Date.now();
    }, [serverNow]);

    const [now, setNow] = useState(() => Date.now() + serverOffset);
    const [finishedCalled, setFinishedCalled] = useState(false);

    useEffect(() => {
        const interval = window.setInterval(() => {
            setNow(Date.now() + serverOffset);
        }, 1000);

        return () => window.clearInterval(interval);
    }, [serverOffset]);

    const targetMs = target.getTime();
    const diffMs = targetMs - now;

    useEffect(() => {
        if (diffMs <= 0 && !finishedCalled) {
            setFinishedCalled(true);
            onFinished?.();
        }
    }, [diffMs, finishedCalled, onFinished]);

    if (!Number.isFinite(serverOffset) || !Number.isFinite(targetMs)) {
        return <span>Časovač se nepodařilo načíst.</span>;
    }

    if (diffMs <= 0) {
        return <span>{finishedText}</span>;
    }

    const totalSeconds = Math.floor(diffMs / 1000);
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    return (
        <div className={className}>
            <span>{prefix}</span>

            <strong>
                {days > 0 && `${days}d `}
                {hours.toString().padStart(2, "0")}:
                {minutes.toString().padStart(2, "0")}:
                {seconds.toString().padStart(2, "0")}
            </strong>
        </div>
    );
}