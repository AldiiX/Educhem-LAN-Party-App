import type {Metadata} from 'next'
import {historyEvents} from '@/data/site'
import shell from '../page-shell.module.scss'
import styles from './history.module.scss'
import {Header} from "@/components/header";
import {Footer} from "@/components/footer";

export const metadata: Metadata = {
    title: 'Historie',
}

export default function() {
    return (
        <>
            <div className={shell.page}>
                <div className={shell.pageHeader}>
                    <span className={shell.eyebrow}>Historie</span>
                    <h1 className={shell.title}>Historie</h1>
                    <p className={shell.description}>
                        Archiv akcí, fotek a záznamů.
                    </p>
                </div>

                <div className={styles.events}>
                    {historyEvents.map((event) => (
                        <section key={`${event.title}-${event.year}`} className={styles.event}>
                            <div className={styles.eventHeader}>
                                <div>
                                    <p className={styles.date}>{event.date}</p>
                                    <h2 className={styles.title}>{event.title} ({event.year})</h2>
                                    <p className={styles.description}>{event.description}</p>
                                </div>
                                {event.links.length > 0 && (
                                    <div className={styles.links}>
                                        {event.links.map((link) => (
                                            <a key={link.href} href={link.href} className={styles.link} target="_blank" rel="noreferrer">
                                                {link.label}
                                            </a>
                                        ))}
                                    </div>
                                )}
                            </div>

                            <div className={styles.masonry}>
                                {event.photos.map((photo) => (
                                    <figure
                                        key={photo.src}
                                        className={`${styles.photo}${photo.tall ? ` ${styles.tall}` : ''}${photo.wide ? ` ${styles.wide}` : ''}`}
                                    >
                                        <img src={photo.src} alt={photo.alt}/>
                                    </figure>
                                ))}
                            </div>
                        </section>
                    ))}
                </div>
            </div>
        </>
    )
}
