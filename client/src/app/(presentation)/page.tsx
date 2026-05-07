import type { Metadata } from 'next';
import Link from 'next/link'
import {siteConfig} from '@/data/site'
import shell from './page-shell.module.scss'
import styles from './page.module.scss'
import {Header} from "@/components/header";
import {Footer} from "@/components/footer";

export default function HomePage() {
    const event = siteConfig.currentEvent

    return (
        <>
            <section className={styles.hero}>
                <div className={`${styles.heroImage} ${styles.heroImageDark}`}>
                    <img src="/images/banner3.jpeg" alt=""/>
                </div>
                <div className={`${styles.heroImage} ${styles.heroImageLight}`}>
                    <img src="/images/history/summer2024/IMG_20240607_195534.jpg" alt=""/>
                </div>
                <div className={styles.heroOverlay}/>
                <div className={styles.heroContent}>
                    <span className={shell.eyebrow}>{event.dateLong}</span>
                    <h1 className={styles.heroTitle}>
                        {event.heroTitle}
                        <span className={styles.heroAccent}>{event.heroAccent}</span>
                    </h1>
                    <p className={styles.heroDescription}>
                        {event.venueFull}. Přijď si zahrát, rezervovat si místo v LAN Party
                        systému a užít letní akci s kamarády.
                    </p>
                    <div className={styles.heroButtons}>
                        {/*<a href="/app/" className={`${shell.button} ${shell.primaryButton}`}>*/}
                        {/*    Vstup do systému*/}
                        {/*</a>*/}
                        <Link href="/reservation" className={`${shell.button} ${shell.primaryButton}`}>
                            Rezervovat místo
                        </Link>
                        <Link href="/info" className={`${shell.button} ${shell.secondaryButton}`}>
                            Zjistit více
                        </Link>
                    </div>
                </div>
            </section>

            <section className={styles.section}>
                <div className={styles.container}>
                    <div className={styles.infoGrid}>
                        <div className={`${shell.card} ${styles.infoCard}`}>
                            <p className={styles.infoKicker}>Datum</p>
                            <p className={styles.infoValue}>{event.dateShort}</p>
                        </div>
                        <div className={`${shell.card} ${styles.infoCard}`}>
                            <p className={styles.infoKicker}>Místo</p>
                            <p className={styles.infoValue}>{event.venueShort}</p>
                        </div>
                        <div className={`${shell.card} ${styles.infoCard}`}>
                            <p className={styles.infoKicker}>Čas</p>
                            <p className={styles.infoValue}>{event.startTime} - {event.endTime}</p>
                        </div>
                        <div className={`${shell.card} ${styles.infoCard}`}>
                            <p className={styles.infoKicker}>Vstupné</p>
                            <p className={styles.infoValue}>{event.fee}</p>
                        </div>
                    </div>

                    <div className={styles.feature}>
                        <img src="/images/img1.jpg"/>
                        <div className={styles.featureCaption}>
                            <strong>Jedna noc, jedna síť, hodně her.</strong>
                            <span>Školní PC, vlastní setupy, společný prostor a grilování v ceně vstupného.</span>
                        </div>
                    </div>

                    <div className={styles.systemPanel}>
                        <div>
                            <p className={styles.systemKicker}>LAN Party systém</p>
                            <h2 className={styles.systemTitle}>Rezervace míst probíhá v systému</h2>
                            <p className={styles.systemText}>
                                Po zaplacení vstupného vám přijdou přístupové údaje do emailu. V systému si potom
                                rezervujete školní počítač nebo místo pro vlastní setup.
                            </p>
                        </div>
                        <a href="/app/reservations" className={`${shell.button} ${shell.primaryButton}`}>
                            Vstup do systému
                        </a>
                    </div>
                </div>
            </section>

            <section className={`${styles.section} ${styles.sectionTint}`}>
                <div className={styles.container}>
                    <h2 className={styles.sectionTitle}>Jak to funguje</h2>
                    <div className={styles.processGrid}>
                        <div className={styles.steps}>
                            <div className={styles.step}>
                                <div className={styles.stepNumber}>1</div>
                                <div>
                                    <h3 className={styles.stepTitle}>Zaplatit vstupné</h3>
                                    <p className={styles.stepText}>
                                        Převodem {event.fee} na účet {event.bankAccount} nebo QR kódem. Do zprávy
                                        napište {event.paymentMessage}.
                                    </p>
                                </div>
                            </div>
                            <div className={styles.homeQr}>
                                <div>
                                    <p className={styles.homeQrKicker}>Platba QR kódem</p>
                                    <p className={styles.homeQrText}>
                                        Naskenujte QR kód a před odesláním zkontrolujte zprávu pro příjemce.
                                        Platba musí být odeslaná do {event.paymentDeadline}.
                                    </p>
                                </div>
                                <img className={styles.homeQrImage} src="/qr.svg" alt="QR kód pro platbu vstupného"/>
                            </div>
                            <div className={styles.step}>
                                <div className={styles.stepNumber}>2</div>
                                <div>
                                    <h3 className={styles.stepTitle}>Obdržet přístup</h3>
                                    <p className={styles.stepText}>
                                        Platby přiřazujeme ručně, takže přístupové údaje do emailu mohou dorazit až do
                                        2 pracovních dnů.
                                    </p>
                                </div>
                            </div>
                            <div className={styles.step}>
                                <div className={styles.stepNumber}>3</div>
                                <div>
                                    <h3 className={styles.stepTitle}>Rezervovat místo</h3>
                                    <p className={styles.stepText}>
                                        V systému si můžete rezervovat školní PC nebo místo pro vlastní setup.
                                    </p>
                                </div>
                            </div>
                            <div className={styles.step}>
                                <div className={styles.stepNumber}>4</div>
                                <div>
                                    <h3 className={styles.stepTitle}>Přijít a užít si</h3>
                                    <p className={styles.stepText}>
                                        {event.startDate} od {event.startTime} dorazte na {event.venueShort}. Odejít můžete kdykoliv, jen to dejte
                                        vědět učiteli.
                                    </p>
                                </div>
                            </div>
                        </div>
                        <div className={styles.setupImage}>
                            <img src="/images/img2.jpg" alt="Herní setup s počítačem"/>
                        </div>
                    </div>
                </div>
            </section>
        </>
    )
}
