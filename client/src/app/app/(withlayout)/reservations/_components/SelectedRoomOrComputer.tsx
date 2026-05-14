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

export const useSelectedRoomOrComputerStore = create<{
    selectedRoomOrComputer: Room | Computer | null,
    setSelectedRoomOrComputer: (roomOrComputer: Room | Computer | null) => void
}>((set) => ({
    selectedRoomOrComputer: null,
    setSelectedRoomOrComputer: (roomOrComputer: Room | Computer | null) => set({ selectedRoomOrComputer: roomOrComputer })
}))


export default function SelectedRoomOrComputer({ reservations }: { reservations: Reservation[] | null }) {
    const { account } = useAuth();

    const roomOrComputer = useSelectedRoomOrComputerStore(state => state.selectedRoomOrComputer)
    const setRoc = useSelectedRoomOrComputerStore(state => state.setSelectedRoomOrComputer);

    const [isComputerReservation, computer] = useMemo(() => {
        const computer = ComputerSchema.safeParse(roomOrComputer);
        return [computer.success, computer.data];
    }, [roomOrComputer]);

    const [isRoomReservation, room] = useMemo(() => {
        const room = RoomSchema.safeParse(roomOrComputer);
        return [room.success, room.data];
    }, [roomOrComputer]);

    const [hasReservation, reservation, reservationProfileIsAnonymous, reservationProfile] = useMemo(() => {
        const reservation = reservations?.find(r => r.computer?.id === roomOrComputer?.id) ?? null;

        return [reservation !== null, reservation, typeof reservation?.profile === "string", typeof reservation?.profile === "string" ? null : reservation?.profile];
    }, [reservations, roomOrComputer]);


    if(!roomOrComputer || !reservations) return null;



    return (
        <div className={style.component}>
            <div className={style.close} onClick={() => setRoc(null)}></div>

            <div className={style.image} style={{ '--image': `url(${roomOrComputer.imageUrl ?? roomOrComputer.imageUrl})`} as React.CSSProperties}></div>

            <div className={style.content}>
                <h2>{roomOrComputer.label ?? roomOrComputer.label ?? roomOrComputer.id}</h2>

                <Switch>
                    <Case when={isComputerReservation}>
                        <If condition={hasReservation}>
                            <If condition={reservationProfileIsAnonymous} as="p">Obsazeno</If>
                            <If condition={!reservationProfileIsAnonymous}>
                                <Link href={`/app/profile/${reservationProfile?.id}`} className={style.profile}>
                                    <Avatar name={reservationProfile?.fullName ?? ""} src={reservationProfile?.avatarUrl} size="24px" />
                                    <p>{reservationProfile?.fullName}</p>
                                    <small>{ reservationProfile?.class }</small>
                                </Link>
                            </If>
                        </If>

                        <If condition={!hasReservation}>
                            <p>Volné</p>
                        </If>
                    </Case>
                </Switch>
            </div>
        </div>
    )
}