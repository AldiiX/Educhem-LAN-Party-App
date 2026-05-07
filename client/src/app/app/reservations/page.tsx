import {Metadata} from "next";
import Client from "@/app/app/reservations/client";

export const metadata: Metadata = {
    title: 'Rezervace',
    description: 'Systém pro správu a organizaci akce, včetně registrace účastníků, správy programů a dalších funkcí.',
}

export default function () {
    return <Client />
}