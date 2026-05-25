"use client"

import { create } from "zustand"
import {Computer, ComputerSchema, Reservation, Room, RoomSchema} from "@/schemas/ReservationSchema";
import style from "./SelectedReservation.module.scss";
import Switch, { Case } from "@/components/util/Switch";
import { useMemo } from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import If from "@/components/util/If";
import {Avatar} from "@/components/Avatar";
import Link from "next/link";
import {useRoomsAndComputers} from "@/app/app/(withlayout)/reservations/_hooks/useRoomsAndComputers";
import {Button} from "@/components/Button";
import {ProfileHoverCard} from "@/components/ProfileHoverCard";
import {toast} from "react-hot-toast"

let isSelectionSuppressionListenerAttached = false;

export const useSelectedRoomOrComputerStore = create<{
    selectedRoomOrComputer: Room | Computer | null,
    setSelectedRoomOrComputer: (roomOrComputer: Room | Computer | null) => void,
    isSelectionSuppressed: boolean,
    suppressSelectionUntilMouseMove: () => void,
}>((set) => ({
    selectedRoomOrComputer: null,
    isSelectionSuppressed: false,
    setSelectedRoomOrComputer: (roomOrComputer: Room | Computer | null) => set({ selectedRoomOrComputer: roomOrComputer }),
    suppressSelectionUntilMouseMove: () => {
        set({ isSelectionSuppressed: true, selectedRoomOrComputer: null });

        if (typeof window === "undefined" || isSelectionSuppressionListenerAttached) return;
        isSelectionSuppressionListenerAttached = true;

        const handler = () => {
            isSelectionSuppressionListenerAttached = false;
            window.removeEventListener("mousemove", handler);
            set({ isSelectionSuppressed: false });
        };

        window.addEventListener("mousemove", handler, { once: true });
    },
}))


export default function SelectedRoomOrComputer({ reservations, reserve, unbook, isReservationMutationPending, reservationsEnabled }: { reservations: Reservation[] | null, reserve: (id: string, type: "room" | "computer") => void, unbook: () => void, isReservationMutationPending: boolean,  reservationsEnabled: boolean}) {
    const { account } = useAuth();

    const roomOrComputer = useSelectedRoomOrComputerStore(state => state.selectedRoomOrComputer)
    const setRoc = useSelectedRoomOrComputerStore(state => state.setSelectedRoomOrComputer);

    const [isComputerReservation, computer] = useMemo(() => {
        const computer = ComputerSchema.safeParse(roomOrComputer);
        return [computer.success, computer.data];
    }, [roomOrComputer]);

    const [isRoomReservation, room, roomReservations] = useMemo(() => {
        const room = RoomSchema.safeParse(roomOrComputer);
        const roomReservations = reservations?.filter(r => r.room?.id === room.data?.id);
        return [room.success, room.data, roomReservations];
    }, [roomOrComputer, reservations]);

    const [hasReservation, reservation, reservationProfileIsAnonymous, reservationProfile] = useMemo(() => {
        const reservation = reservations?.find(r => r.computer?.id === roomOrComputer?.id) ?? null;

        return [reservation !== null, reservation, typeof reservation?.profile === "string", typeof reservation?.profile === "string" ? null : reservation?.profile];
    }, [reservations, roomOrComputer]);

    const [showReserveButton, showDeleteReservationButton] = useMemo(() => {
        let showReserveButton = false;
        let showDeleteReservationButton = false;


        // pokud account nema zapnute rezervace
        if(account?.enableReservations === false) return [false,false];


        // computer rezervace
        if(reservationsEnabled && isComputerReservation && typeof reservation?.profile !== "string" && account && reservation?.profile.id === account?.id) {
            showDeleteReservationButton = true;
        }

        else if(reservationsEnabled && isComputerReservation && !reservations?.find(r => computer?.id === r.computer?.id)) {
            showReserveButton = true;
        }

        // room rezervace
        else if(reservationsEnabled && isRoomReservation && account && roomReservations?.some(r => typeof r.profile !== "string" && r.profile.id === account.id)) {
            showDeleteReservationButton = true;
        }

        else if(reservationsEnabled && isRoomReservation && reservations && reservations.filter(r => r.room?.id === room?.id).length < (room?.capacity ?? 0) && !reservations?.find(r => r.room?.id === room?.id && typeof r.profile !== "string" && r.profile.id === account?.id)) {
            showReserveButton = true;
        }

        return [showReserveButton, showDeleteReservationButton];
    }, [
        account,
        computer,
        isComputerReservation,
        isRoomReservation,
        reservation,
        reservations,
        reservationsEnabled,
        room,
        roomReservations
    ])


    if(!roomOrComputer || !reservations) return null;



    return (
        <div className={style.component}>
            <div className={style.close} onClick={() => setRoc(null)}></div>

            <div className={style.image} style={{ '--image': `url(${roomOrComputer.imageUrl ?? roomOrComputer.imageUrl})`} as React.CSSProperties}></div>

            <div className={style.content}>
                <h2>{roomOrComputer.label ?? roomOrComputer.label ?? roomOrComputer.id}</h2>
                <If condition={isComputerReservation} as="p" className={style.roomDesc}>{ computer?.room?.label }</If>

                <Switch>
                    <Case when={isComputerReservation}>
                        <If condition={hasReservation}>
                            <If condition={reservationProfileIsAnonymous} as="p">Obsazeno</If>
                            <If condition={!reservationProfileIsAnonymous}>
                                {reservationProfile && (
                                    <ProfileHoverCard account={reservationProfile}>
                                        <Link href={`/app/profile/${reservationProfile.id}`} className={style.profile}>
                                            <Avatar name={reservationProfile.fullName} src={reservationProfile.avatarUrl} size="24px" />
                                            <p>{reservationProfile.fullName}</p>
                                            <small>{ reservationProfile.class }</small>
                                        </Link>
                                    </ProfileHoverCard>
                                )}
                            </If>
                        </If>
                    </Case>

                    <Case when={isRoomReservation}>
                        <div className={style.status}>
                            <h3>Rezervace</h3>
                            <p>{ reservations.filter(r => r.room?.id === room?.id ).length} / {room?.capacity}</p>
                        </div>

                        <If condition={account !== null && roomReservations !== null && roomReservations !== undefined && roomReservations.length > 0} as="div" className={style.users}>
                            {
                                roomReservations?.map((rr) => {
                                    if(typeof rr.profile === "string") return;

                                    return <ProfileHoverCard account={rr.profile} key={rr.profile.id}>
                                    <Link className={style.user} href={`/app/profile/${rr.profile.id}`}>
                                        <Avatar name={rr.profile.fullName} src={rr.profile.avatarUrl} size="24px" />
                                        <p>{rr.profile.fullName}</p>
                                        <small>{ rr.profile.class }</small>
                                    </Link>
                                    </ProfileHoverCard>
                                })
                            }
                        </If>
                    </Case>
                </Switch>

                {/*<If condition={!reservationsEnabled}>*/}
                {/*    <div style={{ width: "100%", height: 1, backgroundColor: "var(--border-color)"}}></div>*/}
                {/*    <p>Rezervace jsou momentálně uzavřené.</p>*/}
                {/*</If>*/}

                <If condition={showReserveButton || showDeleteReservationButton}>
                    <div style={{ width: "100%", height: 1, backgroundColor: "var(--border-color)"}}></div>
                    <If condition={showReserveButton}>
                        <If condition={account !== null} fallback={
                            <p>Pro rezervaci <Link href={"/app/login"} style={{ color: "var(--accent-color)"}}>se musíš přihlásit</Link>.</p>
                        }>
                            <Button
                                type={"primary"}
                                icon={"/icons/door.svg"}
                                text="Rezervovat"
                                loading={isReservationMutationPending}
                                disabled={isReservationMutationPending || !reservationsEnabled}
                                onClick={() => {
                                    if (!reservationsEnabled) {
                                        toast.error("Rezervace jsou momentálně zablokované.");
                                        return;
                                    }

                                    reserve(roomOrComputer.id, isComputerReservation ? "computer" : "room");
                                }}
                            />
                        </If>
                    </If>

                    <If condition={showDeleteReservationButton}>
                        <Button
                            type={"secondary"}
                            icon={"/icons/cancel.svg"}
                            text="Zrušit rezervaci"
                            loading={isReservationMutationPending}
                            disabled={isReservationMutationPending || !reservationsEnabled}
                            onClick={() => {
                                if (!reservationsEnabled) {
                                    toast.error("Rezervace jsou momentálně zablokované.");
                                    return;
                                }

                                unbook();
                            }}
                        />
                    </If>
                </If>
            </div>
        </div>
    )
}
