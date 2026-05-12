import Link from 'next/link'
import {siteConfig} from '@/data/site'
import styles from './footer.module.scss'

const footerLinks = siteConfig.navLinks.filter((link) => link.href !== '/')

export function Footer() {
    return (
        <footer className={styles.footer}>
            <div className={styles.inner}>
                <div className={styles.content}>
                    <Link href="/" className={styles.logo}>
                        <span className={styles.logoMark} aria-hidden="true"/>
                        <span>{siteConfig.brandName}</span>
                    </Link>

                    <nav className={styles.links}>
                        {footerLinks.map((link) => (
                            <Link key={link.href} href={link.href} className={styles.link}>
                                {link.label}
                            </Link>
                        ))}
                    </nav>

                    <div className={styles.actions}>
                        <a href="/app/" className={styles.systemLink}>
                            Vstup do systému
                        </a>
                        <a href="/info.pdf" target="_blank" className={styles.download}>
                            Zobrazit info.pdf
                        </a>
                    </div>
                </div>

                <div className={styles.meta}>
                    {/*<p>*/}
                    {/*    Autor webu:{' '}*/}
                    {/*    <Link href={siteConfig.author.href} className={styles.inlineLink}>*/}
                    {/*        {siteConfig.author.name}*/}
                    {/*    </Link>*/}
                    {/*    . Projekt je záměrně vibecoděn kvůli komplexnosti LAN Party systému, který je extrémně*/}
                    {/*    náročný, a veškerý čas jde hlavně do něj.*/}
                    {/*</p>*/}
                    <p>
                        Web je open source:{' '}
                        <a href={siteConfig.repository.href} className={styles.inlineLink} target="_blank" rel="noreferrer">
                            {siteConfig.repository.label}
                        </a>
                    </p>
                </div>

                <p className={styles.note}>
                    {siteConfig.currentEvent.title} - {siteConfig.currentEvent.venueFull}
                </p>
            </div>
        </footer>
    )
}
