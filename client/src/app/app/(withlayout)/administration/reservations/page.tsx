import type {Metadata} from "next";
import {Reservations} from "./client";
import {redirect} from "next/navigation";

export const metadata: Metadata = {
    title: "Rezervace • Administrace",
};

export default function AdministrationReservationsPage() {
    redirect("/app/administration/");
}