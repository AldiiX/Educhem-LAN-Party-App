"use client"

import style from "@/app/app/(withlayout)/reservations/client.module.scss";
import {useState} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import If from "@/components/util/If";
import Link from "next/link";
import {useReservationsHub} from "@/app/app/(withlayout)/reservations/_hooks/useReservationsHub";
import {Avatar} from "@/components/Avatar";
import MovableMap from "@/components/MovableMap";
import {ITHub} from "@/components/reservation_areas/ITHub";
import {SpiralUpper} from "@/components/reservation_areas/SpiralUpper";
import CapacityChart from "@/components/CapacityChart";

const maps = [
    { id: "ithub", name: "IT Hub (Spodní patro)"},
    { id: "spirala", name: "Spirála (Horní patro)"}
]

export default function() {
    const [totalReservationCapacity, setTotalReservationCapacity] = useState<number | null>(null);
    const [selectedTab, setSelectedTab] = useState<string>("ithub");
    const [isLegendCollapsed, setIsLegendCollapsed] = useState(false);
    const { reservations, connectedIds } = useReservationsHub();
    const {account} = useAuth();
    const reservedCount = reservations?.filter(reservation => reservation.computer !== null || reservation.room !== null).length ?? 0;
    const filledCapacityPercentage = Math.min(100, Math.round((reservedCount / 1) * 100));

    return <>
        <h1 className={style.title}>Rezervace</h1>
        <div className={style.tabs}>
            {
                maps.map(map => {
                    return <button type="button" key={map.id} className={selectedTab === map.id ? style.active : ""} onClick={() => setSelectedTab(map.id)}>{ map.name }</button>
                })
            }
        </div>

        <div className={style.flex}>
            <div className={style.left}>
                <MovableMap
                    className={style.map}
                    width={2560}
                    height={1440}
                    initialScale={0.35}
                    minScale={0.2}
                    maxScale={1.35}
                    resetKey={selectedTab}
                    topLeft={
                        <CapacityChart percentage={filledCapacityPercentage} />
                    }
                    bottomLeft={
                        <div className={`${style.legend} ${isLegendCollapsed ? style.collapsed : ""}`}>
                            <button
                                type="button"
                                className={style.legendToggle}
                                onClick={() => setIsLegendCollapsed(value => !value)}
                                aria-expanded={!isLegendCollapsed}
                            >
                                <span>Legenda mapy:</span>
                                <span aria-hidden="true">{isLegendCollapsed ? "+" : "-"}</span>
                            </button>
                            <div className={style.legendItems}>
                                <p><span className={`${style.legendDot} ${style.setup}`} />Volná místnost pro vlastní setup</p>
                                <p><span className={`${style.legendDot} ${style.pc}`} />Volný počítač</p>
                                <p><span className={`${style.legendDot} ${style.unavailable}`} />Obsazeno / Nedostupné</p>
                                <p><span className={`${style.legendDot} ${style.mine}`} />Tvá rezervace</p>
                            </div>
                        </div>
                    }
                    bottomRight={
                        <div className={style.connectedBadge} title="Aktuálně připojeno">
                            <span>{connectedIds ?? "?"}</span>
                            <span className={style.eyeIcon} aria-hidden="true" />
                        </div>
                    }
                >
                    {selectedTab === "ithub" ? <ITHub /> : <SpiralUpper />}
                </MovableMap>
            </div>

            <div className={style.right}>
                <If as="div" condition={account?.enableReservations === false} className={`${style.block} ${style.block0}`}>
                    <h2>Tvůj účet nemá povolené rezervace!</h2>
                    <p>Důvodem může být to, že nemáš zaplacený vstup. Pokud si myslíš, že se jedná o chybu, kontaktuj administrátora.</p>
                </If>

                <div className={`${style.block} ${style.block1}`}>
                    <h2>Statistiky</h2>
                    <p>
                        Počet rezervovaných PC:
                        <span>{reservations?.filter(r => r.computer !== null).length }/00</span>
                    </p>
                    <p>
                        Počet rezervovaných míst:
                        <span>{reservations?.filter(r => r.room !== null).length }/00</span>
                    </p>
                    <p>
                        Celkem rezervací:
                        <span>{(reservations?.filter(r => r.computer !== null).length ?? 0) + (reservations?.filter(r => r.room !== null).length ?? 0) }/00</span>
                    </p>
                </div>

                <div className={`${style.block} ${style.block2} ${account !== null ? style.loggedIn : ''}`}>
                    <h2>Seznam rezervací</h2>

                    <div className={style.content}>
                        <If condition={account === null}>
                            <p>Pro zobrazení seznamu <Link href="/app/login">se prosím přihlaš</Link>.</p>
                        </If>

                        <If condition={account !== null}>
                            {
                                reservations?.map(reservation => {
                                    if(typeof(reservation.profile) === "string") return null;

                                    return <Link href={"/app/profile/" + reservation.profile.id} className={style.child} key={reservation.profile.id}>
                                        <Avatar name={reservation.profile.fullName} size="32px" src={reservation.profile.avatarUrl} />
                                        <p>{ reservation.profile.fullName }</p>
                                        <p>{ reservation.profile.class}</p>
                                        <p>{ reservation.computer?.label ?? reservation.room?.label}</p>
                                        <p>{ (reservation.createdAtUtc as Date).toLocaleString() }</p>
                                    </Link>
                                })
                            }
                        </If>
                    </div>

                </div>
            </div>
        </div>
    </>
}
