import style from "./TournamentCard.module.scss";
import type {Tournament} from "../_hooks/useTournaments";

export function TournamentCard({tournament}: {tournament: Tournament}) {
    return <article className={style.card}>
        <div className={style.header}>
            <span style={{maskImage: `url(${tournament.icon})`}}></span>
            <div>
                <h2>{tournament.name}</h2>
                <p>{tournament.game}</p>
            </div>
        </div>

        <p className={style.description}>{tournament.description}</p>

        <dl className={style.details}>
            <div>
                <dt>Termín</dt>
                <dd>{tournament.date}</dd>
            </div>
            <div>
                <dt>Týmy</dt>
                <dd>{tournament.teamSize}</dd>
            </div>
            <div>
                <dt>Kapacita</dt>
                <dd>{tournament.registered}/{tournament.capacity}</dd>
            </div>
        </dl>

        <button type="button" disabled>{tournament.actionLabel}</button>
    </article>;
}
