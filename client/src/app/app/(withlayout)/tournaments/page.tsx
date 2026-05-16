import type {Metadata} from "next";
import TournamentsClient from "./client";

export const metadata: Metadata = {
    title: "Turnaje",
};

export default function TournamentsPage() {
    return <TournamentsClient />;
}
