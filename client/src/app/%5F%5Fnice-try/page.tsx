import type { Metadata } from 'next';
import styles from "./nice-try.module.scss";

export const metadata: Metadata = {
    title: "Nevyšlo...                                                             ",
    description: "Pokus dobrý, ale zkus to spíš tady.",
}

export default function() {
    return (
        <main className={styles.page}>
            <section className={styles.card}>
                <img alt="Honza Kerkel" src="https://cloud02.emsio.cz/public/img/kekel.jpg" width={180} style={{objectFit: "fill", aspectRatio: 1}} />
                <p>Pokus dobrý, ale zkus to spíš tady.</p>
                <a href="https://pradelnamost.cz" className={styles.button}>
                    Prádelna Most
                </a>
            </section>
        </main>
    );
}
