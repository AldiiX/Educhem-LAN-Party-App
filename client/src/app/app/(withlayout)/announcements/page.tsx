import type {Metadata} from "next";
import AnnouncementsClient from "./client";

export const metadata: Metadata = {
    title: "Oznámení",
};

export default function AnnouncementsPage() {
    return <AnnouncementsClient />;
}
