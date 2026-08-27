import type {Metadata} from "next";
import style from "./layoutclient.module.scss";

export const metadata: Metadata = {
    title: "Přehled administrace",
};

export default function AdministrationOverviewPage() { // !!! STRANKA JE VYPLA V NEXT.CONFIG.TS
    return <section className={style.overview}>
        <h2>Přehled</h2>
        <p>Přehled administrace zatím připravujeme.</p>
    </section>;
}

