"use client";

import {useMemo} from "react";
import {vocative} from "czech-vocative";

export function useHomeGreeting(name: string | null) {
    return useMemo(() => {
        const hour = new Date().getHours();
        const suffix = name ? `, ${getVocativeName(name)}` : "";

        if(hour >= 5 && hour < 11) return `Dobré ráno${suffix}`;
        if(hour >= 11 && hour < 18) return `Dobrý den${suffix}`;
        return `Dobrý večer${suffix}`;
    }, [name]);
}

function getVocativeName(name: string) {
    const firstName = name.trim().split(/\s+/)[0];
    if(!firstName) return name;

    try {
        return vocative(firstName) || firstName;
    } catch {
        return firstName;
    }
}
