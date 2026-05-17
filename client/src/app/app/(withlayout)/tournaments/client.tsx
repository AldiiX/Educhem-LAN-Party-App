"use client";

import style from "./client.module.scss";
import {TournamentCard} from "./_components/TournamentCard";
import {useTournaments} from "./_hooks/useTournaments";


type TournamentGame = {
    id: string;
    name: string;
    url: string;
    backgroundImage?: string;
    enabled?: boolean,
};

const tournamentGames: TournamentGame[] = [ // TODO: udělat aby to šlo z db a ne hardcodnuty
    {
        id: "cs2",
        name: "Counter-Strike 2",
        url: "https://www.copafacil.com/-v9yer2",
        backgroundImage: 'https://i.imgur.com/hjpEYZz.png',
        enabled: true,
    },
    {
        id: "valorant",
        name: "VALORANT",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://www.riotgames.com/darkroom/1440/8d5c497da1c2eeec8cffa99b01abc64b:5329ca773963a5b739e98e715957ab39/ps-f2p-val-console-launch-16x9.jpg',
    },
    {
        id: "lol",
        name: "League of Legends",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://egw.news/_next/image?url=https%3A%2F%2Fegw.news%2Fuploads%2Fnews%2F1%2F17%2F1739981305866_1739981305866.webp&w=1920&q=75',
    },
    {
        id: "rocket-league",
        name: "Rocket League",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://cdn1.epicgames.com/offer/9773aa1aa54f4f7b80e44bef04986cea/EGS_RocketLeague_PsyonixLLC_S1_2560x1440-4c231557ef0a0626fbb97e0bd137d837',
    },
    {
        id: "r6",
        name: "Tom Clancy's Rainbow Six® Siege X",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://staticctf.ubisoft.com/J3yJr34U2pZ2Ieem48Dwy9uqj5PNUQTn/3xF5wFU6mfUv1weqYKOfgT/7389f6f1b2c2af7a2eaa3dbb04cbbf85/UBI-R6X_KA-R16_2D-RevisedFinal_Horizontal-960x540.jpg',
    },
    // {
    //     id: "minecraft",
    //     name: "Minecraft (BedWars / minihry)",
    //     url: "https://www.copafacil.com/",
    //     backgroundImage: 'https://playhive.com/content/images/2024/03/bedwars_release5.png',
    // },
    // {
    //     id: "brawl-stars",
    //     name: "Brawl Stars",
    //     url: "https://www.copafacil.com/",
    //     backgroundImage: 'https://playhive.com/content/images/2024/03/bedwars_release5.png',
    // },
    {
        id: "overwatch2",
        name: "Overwatch 2",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://static0.gamerantimages.com/wordpress/wp-content/uploads/wm/2024/08/overwatch-2-season-12-banner.jpg',
        enabled: false
    },

    {
        id: "fortnite",
        name: "Fortnite",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://cdn2.unrealengine.com/fneco-2025-keyart-thumb-1920x1080-de84aedabf4d.jpg',
        enabled: false
    },

    {
        id: "apex",
        name: "Apex Legends",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://alegends.gg/wp-content/uploads/2025/08/70e4ed688f5e6c4ce2936d7ba8fddc1fb35c5c21-1920x1080-1.webp',
        enabled: false
    },

    {
        id: "fallguys",
        name: "Fall Guys",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://cdn.alza.cz/Foto/ImgGalery/Image/fall-guys.jpg',
        enabled: false
    },

    {
        id: "pubg",
        name: "PUBG: Battlegrounds",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://cdn.alza.cz/Foto/ImgGalery/Image/pubg-battlegrounds-keyart.jpg',
        enabled: false
    },

    {
        id: "tf2",
        name: "Team Fortress 2",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://gaming-cdn.com/images/products/18886/orig-fallback-v1/team-fortress-2-pc-game-steam-cover.jpg?v=1742298289',
        enabled: false
    },

    {
        id: "dbd",
        name: "Dead by Daylight",
        url: "https://www.copafacil.com/",
        backgroundImage: 'https://www.nintendo.com/eu/media/images/10_share_images/games_15/nintendo_switch_4/H2x1_NSwitch_DeadByDaylight_image1600w.jpg',
        enabled: false
    },
];





export default function() {
    const {tournaments, selectedState, setSelectedState, states} = useTournaments();

    return <main className={style.tournaments}>
        <h1>Turnaje</h1>

        <div className={style.tournamentsPage}>
            <section className={style.grid}>
                {tournamentGames.sort((a,b) => a.id.localeCompare(b.id)).sort((a,b) => a.enabled ? -1 : 1).map((game) => (
                    <a
                        key={game.id}
                        href={game.url}
                        target="_blank"
                        rel="noreferrer"
                        className={style.card + " " + (!game.enabled ? style.disabled : "")}
                    >
                        <div className={style.bg} style={{ backgroundImage: `url(${game.backgroundImage})` }}></div>

                        <div className={style.content}>
                            <span className={style.gameName}>{game.name}</span>
                            <span className={style.externalIcon} aria-hidden="true" />
                        </div>
                    </a>
                ))}
            </section>
        </div>

        {/*<section className={style.hero}>*/}
        {/*    <div>*/}
        {/*        <span style={{maskImage: "url(/icons/trophy_star.svg)"}}></span>*/}
        {/*        <div>*/}
        {/*            <h2>LAN Party turnaje</h2>*/}
        {/*            <p>Přehled plánovaných soutěží, registrací a pravidel pro účastníky.</p>*/}
        {/*        </div>*/}
        {/*    </div>*/}
        {/*</section>*/}

        {/*<div className={style.tabs}>*/}
        {/*    {states.map(state => (*/}
        {/*        <button*/}
        {/*            key={state.value}*/}
        {/*            type="button"*/}
        {/*            className={selectedState === state.value ? style.active : ""}*/}
        {/*            onClick={() => setSelectedState(state.value)}*/}
        {/*        >*/}
        {/*            {state.label}*/}
        {/*        </button>*/}
        {/*    ))}*/}
        {/*</div>*/}

        {/*<section className={style.grid}>*/}
        {/*    {tournaments.map(tournament => (*/}
        {/*        <TournamentCard key={tournament.id} tournament={tournament} />*/}
        {/*    ))}*/}
        {/*</section>*/}
    </main>;
}
