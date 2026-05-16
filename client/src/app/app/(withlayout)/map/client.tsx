"use client"

import style from "@/app/app/(withlayout)/reservations/client.module.scss";
import {useCallback, useState, useEffect, useMemo} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import If from "@/components/util/If";
import Link from "next/link";
import {useReservationsHub} from "@/app/app/(withlayout)/reservations/_hooks/useReservationsHub";
import {Avatar} from "@/components/Avatar";
import MovableMap from "@/components/MovableMap";
import {ITHub} from "@/components/reservation_areas/ITHub";
import {SpiralUpper} from "@/components/reservation_areas/SpiralUpper";
import CapacityChart from "@/components/CapacityChart";
import {useRememberState} from "@/hooks/useRememberState";
import {useRoomsAndComputers} from "@/app/app/(withlayout)/reservations/_hooks/useRoomsAndComputers";
import {useReservationsDisplay} from "@/app/app/(withlayout)/reservations/_hooks/useReservationsDisplay";
import Switch, {Case} from "@/components/util/Switch";
import SelectedRoomOrComputer from "@/app/app/(withlayout)/reservations/_components/SelectedRoomOrComputer";
import {maps} from "@/app/app/(withlayout)/reservations/client";

export default function() {
    const [selectedTab, setSelectedTab] = useState<string>("ithub");
    const {account} = useAuth();

    return <>
        <h1 className={style.title}>Mapa</h1>
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
                >
                    { selectedTab === "ithub" ?
                        <ITHub
                            getComputerClass={() => ""}
                            getRoomClass={() => ""}
                            onHoverReservation={() => {}}
                        />
                        :
                        <SpiralUpper
                            getComputerClass={() => ""}
                            getRoomClass={() => ""}
                            onHoverReservation={() => {}}
                        />
                    }
                </MovableMap>
            </div>
        </div>
    </>
}
