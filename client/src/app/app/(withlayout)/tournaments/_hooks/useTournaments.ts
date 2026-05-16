"use client";

import {useMemo, useState} from "react";

export type TournamentState = "all" | "open" | "prepared";

export type Tournament = {
    id: string;
    name: string;
    game: string;
    date: string;
    teamSize: string;
    capacity: number;
    registered: number;
    state: Exclude<TournamentState, "all">;
    icon: string;
    description: string;
    actionLabel: string;
};

const tournamentData: Tournament[] = [
    {
        id: "cs2",
        name: "CS2 Wingman",
        game: "Counter-Strike 2",
        date: "Bude upřesněno",
        teamSize: "2v2",
        capacity: 16,
        registered: 0,
        state: "prepared",
        icon: "/icons/trophy_star.svg",
        description: "Krátký týmový turnaj pro dvojice. Finální pravidla a registrace budou doplněny před akcí.",
        actionLabel: "Registrace se připravuje",
    },
    {
        id: "minecraft",
        name: "Minecraft výzva",
        game: "Minecraft",
        date: "Bude upřesněno",
        teamSize: "Solo",
        capacity: 24,
        registered: 0,
        state: "prepared",
        icon: "/icons/computer.svg",
        description: "Volnější soutěžní režim pro rychlé stavění nebo minihry podle programu LAN Party.",
        actionLabel: "Registrace se připravuje",
    },
    {
        id: "rocket-league",
        name: "Rocket League",
        game: "Rocket League",
        date: "Bude upřesněno",
        teamSize: "2v2",
        capacity: 12,
        registered: 0,
        state: "prepared",
        icon: "/icons/calendar.svg",
        description: "Rychlý turnaj v krátkých zápasech. Formát se upraví podle počtu přihlášených hráčů.",
        actionLabel: "Registrace se připravuje",
    },
];

export function useTournaments() {
    const [selectedState, setSelectedState] = useState<TournamentState>("all");
    const states = [
        {value: "all" as const, label: "Vše"},
        {value: "open" as const, label: "Otevřené"},
        {value: "prepared" as const, label: "Připravuje se"},
    ];

    const tournaments = useMemo(() => {
        if(selectedState === "all") return tournamentData;
        return tournamentData.filter(tournament => tournament.state === selectedState);
    }, [selectedState]);

    return {
        tournaments,
        selectedState,
        setSelectedState,
        states,
    };
}
