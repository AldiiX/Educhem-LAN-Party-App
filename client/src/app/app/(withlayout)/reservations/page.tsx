import {Metadata} from "next";
import {cookies} from "next/headers";
import Client from "@/app/app/(withlayout)/reservations/client";

export const metadata: Metadata = {
    title: 'Rezervace',
    description: 'Systém pro správu a organizaci akce, včetně registrace účastníků, správy programů a dalších funkcí.',
}

const readCollapsedPreference = async (name: string) => {
    const cookieStore = await cookies();
    return cookieStore.get(name)?.value === "true";
}

export default async function () {
    const [isRightPanelCollapsed, isLegendCollapsed] = await Promise.all([
        readCollapsedPreference("reservationsRightPanelCollapsed"),
        readCollapsedPreference("reservationsLegendCollapsed"),
    ]);

    return (
        <Client
            initialRightPanelCollapsed={isRightPanelCollapsed}
            initialLegendCollapsed={isLegendCollapsed}
        />
    )
}
