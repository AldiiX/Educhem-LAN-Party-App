"use client"

import style from "@/app/app/(withlayout)/reservations/client.module.scss";
import { useState } from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import If from "@/components/util/If";
import Link from "next/link";
import {useReservationsHub} from "@/app/app/(withlayout)/reservations/_hooks/useReservationsHub";
import {Avatar} from "@/components/Avatar";

const maps = [
    { id: "ithub", name: "IT Hub (Spodní patro)"},
    { id: "spirala", name: "Spirála (Horní patro)"}
]

export default function() {
    const [selectedTab, setSelectedTab] = useState<string>("ithub");
    const { reservations, sendMessage, isConnected, connectedIds } = useReservationsHub();
    const {account} = useAuth();

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
                {/*{ JSON.stringify(reservations) }*/}

                <If condition={isConnected}>
                    <p>connected</p>
                </If>

                <If condition={!isConnected}>
                    <p>disconnected</p>
                </If>
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