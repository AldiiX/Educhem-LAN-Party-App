import type {Metadata} from "next";
import Link from "next/link";
import {Button} from "@/components/Button";
import styles from "./page.module.scss";
import {Header} from "@/components/header";
import {Footer} from "@/components/footer";

export const metadata: Metadata = {
    title: "Systém je vypnutý",
};

export default function SystemDisabledPage() {
    return (
        <>
            <Header />

            <main className={styles.page}>
                <section className={styles.panel}>
                    <p className={styles.kicker}>Educhem LAN Party</p>
                    <h1>Systém je aktuálně vypnutý</h1>
                    <p className={styles.message}>
                        Přihlášení, reset hesla a vnitřní část systému jsou dočasně nedostupné.
                    </p>
                    <Link href="/">
                        <Button type="secondary" text="Zpět na hlavní stránku" />
                    </Link>
                </section>
            </main>

            <Footer />
        </>
    );
}
