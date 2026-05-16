"use client";

import style from "./client.module.scss";
import {TournamentCard} from "./_components/TournamentCard";
import {useTournaments} from "./_hooks/useTournaments";

export default function TournamentsClient() {
    const {tournaments, selectedState, setSelectedState, states} = useTournaments();

    return <main className={style.tournaments}>
        <h1>Turnaje</h1>

        <section className={style.hero}>
            <div>
                <span style={{maskImage: "url(/icons/trophy_star.svg)"}}></span>
                <div>
                    <h2>LAN Party turnaje</h2>
                    <p>Přehled plánovaných soutěží, registrací a pravidel pro účastníky.</p>
                </div>
            </div>
        </section>

        <div className={style.tabs}>
            {states.map(state => (
                <button
                    key={state.value}
                    type="button"
                    className={selectedState === state.value ? style.active : ""}
                    onClick={() => setSelectedState(state.value)}
                >
                    {state.label}
                </button>
            ))}
        </div>

        <section className={style.grid}>
            {tournaments.map(tournament => (
                <TournamentCard key={tournament.id} tournament={tournament} />
            ))}
        </section>
    </main>;
}
