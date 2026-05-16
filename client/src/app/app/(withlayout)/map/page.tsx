import {Metadata} from "next";
import Client from "@/app/app/(withlayout)/map/client";

export const metadata: Metadata = {
    title: 'Mapa',
    description: 'Interaktivní mapa s obsazeností místností a počítačů.',
}

export default async function () {
    return <Client />
}
