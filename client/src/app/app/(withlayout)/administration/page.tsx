import type {Metadata} from "next";
import style from "./client.module.scss";
import {requireAdministrationRole} from "./_lib/requireAdministrationRole";
import { redirect } from "next/navigation";

export const metadata: Metadata = {
    title: "Přehled administrace",
};

export default async function AdministrationOverviewPage() { // !!! STRANKA JE VYPLA V NEXT.CONFIG.TS
    await requireAdministrationRole("TeacherOrg");

    return <section className={style.overview}>
        <h2>Přehled</h2>
        <p>Přehled administrace zatím připravujeme.</p>
    </section>;
}
