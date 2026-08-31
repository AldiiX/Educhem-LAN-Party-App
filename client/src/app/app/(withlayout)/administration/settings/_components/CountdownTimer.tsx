"use client";

import {useEffect, useState} from "react";
import {useServerCountdown} from "@/hooks/useServerCountdown";

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
    const [finishedCalled, setFinishedCalled] = useState(false);
    const countdown = useServerCountdown(serverNow, target);

    useEffect(() => {
        if (countdown.isFinished && !finishedCalled) {
            setFinishedCalled(true);
            onFinished?.();
        }
    }, [countdown.isFinished, finishedCalled, onFinished]);

    if (!countdown.isValid) {
        return <span>Časovač se nepodařilo načíst.</span>;
    }

    if (countdown.isFinished) {
        return <span>{finishedText}</span>;
    }

    return (
        <div className={className}>
            <span>{prefix}</span>
            <strong>{countdown.formatted}</strong>
        </div>
    );
}
