import type {Metadata} from "next";
import {redirect} from "next/navigation";

export const metadata: Metadata = {
    title: "Správa achievementů • Administrace",
};

export default function AdministrationAchievementsPage() {
    redirect("/app/administration/");
}

