"use client";

import style from "./client.module.scss";
import {EmptyAnnouncements} from "./_components/EmptyAnnouncements";
import {useAnnouncements} from "./_hooks/useAnnouncements";

export default function AnnouncementsClient() {
    const {announcements} = useAnnouncements();

    return <main className={style.announcements}>
        <h1>Oznámení</h1>
        {announcements.length === 0 && <EmptyAnnouncements />}
    </main>;
}
