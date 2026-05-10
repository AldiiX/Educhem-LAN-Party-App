"use client";

import {useMemo} from "react";

export function useHomeGreeting(name: string | null) {
    return useMemo(() => {
        const hour = new Date().getHours();
        const suffix = name ? `, ${name}` : "";

        if(hour >= 5 && hour < 11) return `Dobré ráno${suffix}`;
        if(hour >= 11 && hour < 18) return `Dobrý den${suffix}`;
        return `Dobrý večer${suffix}`;
    }, [name]);
}
