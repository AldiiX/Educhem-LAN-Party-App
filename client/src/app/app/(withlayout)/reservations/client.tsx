"use client"

import style from "@/app/app/(withlayout)/reservations/client.module.scss";
import {useCallback, useEffect, useMemo, useState} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import If from "@/components/util/If";
import Link from "next/link";
import {useReservationsHub} from "@/app/app/(withlayout)/reservations/_hooks/useReservationsHub";
import {Avatar} from "@/components/Avatar";
import MovableMap from "@/components/MovableMap";
import {ITHub} from "@/components/reservation_areas/ITHub";
import {SpiralUpper} from "@/components/reservation_areas/SpiralUpper";
import CapacityChart from "@/components/CapacityChart";
import {useRoomsAndComputers} from "@/app/app/(withlayout)/reservations/_hooks/useRoomsAndComputers";
import {useReservationsDisplay} from "@/app/app/(withlayout)/reservations/_hooks/useReservationsDisplay";
import Switch, {Case} from "@/components/util/Switch";
import SelectedRoomOrComputer, {useSelectedRoomOrComputerStore} from "@/app/app/(withlayout)/reservations/_components/SelectedRoomOrComputer";
import {ProfileHoverCard, closeProfileHoverImmediate} from "@/components/ProfileHoverCard";
import {useReservationStatus} from "@/app/app/(withlayout)/reservations/_hooks/useReservationStatus";
import {ReservationCountdownStatus} from "@/app/app/(withlayout)/reservations/_components/ReservationCountdownStatus";
import {useLiveReservationsEnabled} from "@/app/app/(withlayout)/reservations/_hooks/useLiveReservationsEnabled";
import {useRememberedCollapseState} from "@/app/app/(withlayout)/reservations/_hooks/useRememberedCollapseState";
import {useReservationStats} from "@/app/app/(withlayout)/reservations/_hooks/useReservationStats";

export const maps = [
    { id: "ithub", name: "IT Hub (Spodní patro)"},
    { id: "spirala", name: "Spirála (Horní patro)"}
]

type ClientProps = {
    initialRightPanelCollapsed?: boolean;
    initialLegendCollapsed?: boolean;
};

export default function Client({
    initialRightPanelCollapsed = false,
    initialLegendCollapsed = false,
}: ClientProps) {
    const [selectedTab, setSelectedTab] = useState<string>("ithub");
    const setSelectedRoomOrComputer = useSelectedRoomOrComputerStore(state => state.setSelectedRoomOrComputer);
    const [isRightPanelCollapsed, toggleRightPanelCollapsed] = useRememberedCollapseState(
        initialRightPanelCollapsed,
        "reservationsRightPanelCollapsed",
    );
    const [isLegendCollapsed, toggleLegendCollapsed] = useRememberedCollapseState(
        initialLegendCollapsed,
        "reservationsLegendCollapsed",
    );

    const { reservations, connectedIds, isConnected, isDisconnected, isConnecting, isReconnecting, reserve, unbook, isReservationMutationPending } = useReservationsHub();
    const { roomsCapacity, maxCapacity, computersCapacity, rooms, computers } = useRoomsAndComputers();
    const {reservationStatus, mutateReservationStatus} = useReservationStatus();
    const refreshReservationStatus = useCallback(() => {
        void mutateReservationStatus();
    }, [mutateReservationStatus]);
    const reservationsEnabled = useLiveReservationsEnabled(reservationStatus, refreshReservationStatus);
    const isConnectionLost = useMemo(() =>{
        return isReconnecting || isDisconnected;
    }, [isDisconnected, isReconnecting])
    const reservationDisplay = useReservationsDisplay(rooms, computers, reservations);
    const {account} = useAuth();
    const reservationStats = useReservationStats(reservations, computersCapacity, roomsCapacity, maxCapacity);

    useEffect(() => {
        closeProfileHoverImmediate();
        setSelectedRoomOrComputer(null);
    }, [selectedTab, setSelectedRoomOrComputer]);

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
                <If condition={reservationStatus !== null}>
                    <ReservationCountdownStatus
                        status={reservationStatus!}
                        reservationsEnabled={reservationsEnabled}
                        className={style.reservationStatus}
                    />
                </If>

                <MovableMap
                    className={style.map}
                    width={2560}
                    height={1440}
                    initialScale={0.35}
                    minScale={0.2}
                    maxScale={1.35}
                    resetKey={selectedTab}
                    topLeft={
                        <div className={style.mapTopLeft}>
                            <CapacityChart percentage={!isConnectionLost ? reservationStats.filledCapacityPercentage : 0} />
                        </div>
                    }
                    bottomLeft={
                        <div className={`${style.legend} ${isLegendCollapsed ? style.collapsed : ""}`}>
                            <button
                                type="button"
                                className={style.legendToggle}
                                onClick={toggleLegendCollapsed}
                                aria-expanded={!isLegendCollapsed}
                                aria-label={isLegendCollapsed ? "Zobrazit legendu mapy" : "Skrýt legendu mapy"}
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
                        <div className={style.statusBadges}>
                            <Switch>
                                <Case when={isReconnecting} as="div" className={`${style.badge} ${style.disconnected}`}>
                                    <div className={style.icon} style={{ '--icon': 'url(/icons/loading.svg)'} as React.CSSProperties}></div>
                                    <p>Připojení bylo ztraceno! Připojování...</p>
                                </Case>

                                <Case when={isDisconnected} as="div" className={`${style.badge} ${style.disconnected}`}>
                                    <div className={style.icon} style={{ '--icon': 'url(/icons/disconnected.svg)'} as React.CSSProperties}></div>
                                    <p>Připojení bylo ztraceno! Obnovte stránku.</p>
                                </Case>

                                <Case when={isConnecting || reservations === null} as="div" className={`${style.badge} ${style.connecting}`}>
                                    <div className={style.icon} style={{ '--icon': 'url(/icons/loading.svg)'} as React.CSSProperties}></div>
                                    <p>Připojování na server...</p>
                                </Case>


                                <Case when={isConnected} as="div" className={`${style.badge} ${style.connected}`}>
                                    <div className={style.icon} style={{ '--icon': 'url(/icons/successmark.svg)'} as React.CSSProperties}></div>
                                    <p>Úspěšně připojeno k serveru</p>
                                </Case>
                            </Switch>

                            <div className={style.connectedBadge} title="Aktuálně připojeno">
                                <span>{!isConnectionLost ? connectedIds ?? "?" : "?"}</span>
                                <span className={style.eyeIcon} aria-hidden="true" />
                            </div>
                        </div>
                    }
                >
                    { selectedTab === "ithub" ?
                        <ITHub
                            getComputerClass={isConnectionLost ? () => "" : reservationDisplay.getComputerClass}
                            getRoomClass={isConnectionLost ? () => "" : reservationDisplay.getRoomClass}
                            onHoverReservation={isConnectionLost ? () => {} : reservationDisplay.openReservationModal}
                        />
                        :
                        <SpiralUpper
                            getComputerClass={isConnectionLost ? () => "" : reservationDisplay.getComputerClass}
                            getRoomClass={isConnectionLost ? () => "" : reservationDisplay.getRoomClass}
                            onHoverReservation={isConnectionLost ? () => {} : reservationDisplay.openReservationModal}
                        />
                    }
                </MovableMap>

                <SelectedRoomOrComputer reservations={reservations} reserve={reserve} unbook={unbook} isReservationMutationPending={isReservationMutationPending} reservationsEnabled={reservationsEnabled}/>
            </div>

            <aside className={`${style.right} ${isRightPanelCollapsed ? style.collapsed : ""}`}>
                <button
                    type="button"
                    className={style.rightToggle}
                    onClick={toggleRightPanelCollapsed}
                    aria-expanded={!isRightPanelCollapsed}
                    aria-label={isRightPanelCollapsed ? "Zobrazit pravý panel" : "Skrýt pravý panel"}
                >
                    <span aria-hidden="true">{isRightPanelCollapsed ? "«" : "»"}</span>
                </button>

                <div className={style.rightContent} aria-hidden={isRightPanelCollapsed}>
                    <Switch>
                        <Case when={account?.enableReservations === false} as="div" className={`${style.block} ${style.block0}`}>
                            <h2>Tvůj účet nemá povolené rezervace!</h2>
                            <p>Důvodem může být to, že nemáš zaplacený vstup. Pokud si myslíš, že se jedná o chybu, kontaktuj administrátora.</p>
                        </Case>

                        <Case when={reservationStatus !== null && !reservationsEnabled} as="div" className={`${style.block} ${style.block0}`}>
                            <h2>Rezervace jsou uzavřené</h2>
                            <p>Momentálně není možné vytvářet nové rezervace.</p>
                        </Case>
                    </Switch>


                    <div className={`${style.block} ${style.blockMeta}`}>
                        <CapacityChart
                            percentage={!isConnectionLost ? reservationStats.filledCapacityPercentage : 0}
                            className={style.compactCapacity}
                        />
                        <If condition={reservationStatus !== null}>
                            <ReservationCountdownStatus
                                status={reservationStatus!}
                                reservationsEnabled={reservationsEnabled}
                                className={style.inlineReservationStatus}
                            />
                        </If>
                    </div>

                    <div className={`${style.block} ${style.block1}`}>
                        <h2>Statistiky</h2>
                        <p>
                            Počet rezervovaných PC:
                            <span>{reservationStats.reservedComputers}/{reservationStats.computersCapacity}</span>
                        </p>
                        <p>
                            Počet rezervovaných míst:
                            <span>{reservationStats.reservedRooms}/{reservationStats.roomsCapacity}</span>
                        </p>
                        <p>
                            Celkem rezervací:
                            <span>{reservationStats.reservedCount}/{reservationStats.maxCapacity}</span>
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

                                        return <ProfileHoverCard account={reservation.profile} key={reservation.profile.id}>
                                        <Link href={"/app/profile/" + reservation.profile.id} className={style.child}>
                                            <If condition={Boolean(reservation.profile.bannerUrl)} as="span" className={style.bannerBackdrop} style={{backgroundImage: `url(${reservation.profile.bannerUrl})`}} />
                                            <Avatar name={reservation.profile.fullName} size="40px" src={reservation.profile.avatarUrl} />
                                            <span>
                                                <strong>{ reservation.profile.fullName }<If as="small" condition={reservation.profile.class !== null}>{ reservation.profile.class}</If></strong>
                                                <If condition={reservation.computer !== null} as="span">{ reservation.computer?.label } - {reservation.computer?.room?.id}</If>
                                                <If condition={reservation.room !== null} as="span">{ reservation.room?.label }</If>
                                                <time>{ (reservation.createdAtUtc as Date).toLocaleString() }</time>
                                            </span>
                                        </Link>
                                        </ProfileHoverCard>
                                    })
                                }
                            </If>
                        </div>

                    </div>
                </div>
            </aside>
        </div>
    </>
}
