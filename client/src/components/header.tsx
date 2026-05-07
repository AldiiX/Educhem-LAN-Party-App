'use client'

import Link from 'next/link'
import {usePathname} from 'next/navigation'
import {useState} from 'react'
import {siteConfig} from '@/data/site'
import styles from './navbar.module.scss'
import {useWebTheme} from "@/app/_providers/WebThemeProvider";

export function Header() {
    const pathname = usePathname()
    const [open, setOpen] = useState(false)
    const {theme, toggleTheme} = useWebTheme();

    return (
        <>
            <header className={styles.header}>
                <Link href="/" className={styles.logo}>
                    <span className={styles.logoMark} aria-hidden="true"/>
                    <span>{siteConfig.brandName}</span>
                </Link>

                <nav className={styles.nav}>
                    {siteConfig.navLinks.map((link) => (
                        <Link
                            key={link.href}
                            href={link.href}
                            className={`${styles.navLink}${pathname === link.href ? ` ${styles.active}` : ''}`}
                        >
                            {link.label}
                        </Link>
                    ))}
                </nav>

                <div className={styles.actions}>
                    <a href="/app/reservations" className={styles.systemLink}>
                        Vstup do systému
                    </a>
                    <button
                        onClick={toggleTheme}
                        className={styles.iconButton}
                        aria-label="Přepnout barevný režim"
                        title={theme === 'dark' ? 'Přepnout na světlý režim' : 'Přepnout na tmavý režim'}
                    >
                        <span className={styles.themeIcon} aria-hidden="true"/>
                    </button>
                    <button
                        className={`${styles.iconButton} ${styles.mobileButton}`}
                        onClick={() => setOpen(! open)}
                        aria-label={open ? 'Zavřít menu' : 'Otevřít menu'}
                        aria-expanded={open}
                    >
                        <span className={styles.menuIcon} aria-hidden="true"/>
                    </button>
                </div>
            </header>

            <div className={`${styles.mobileMenu}${open ? ` ${styles.mobileMenuOpen}` : ''}`}>
                <a href="/app/reservations" className={styles.mobileSystemLink}>
                    Vstup do systému
                </a>
                {siteConfig.navLinks.map((link) => (
                    <Link
                        key={link.href}
                        href={link.href}
                        onClick={() => setOpen(false)}
                        className={`${styles.mobileLink}${pathname === link.href ? ` ${styles.active}` : ''}`}
                    >
                        {link.label}
                    </Link>
                ))}
            </div>
        </>
    )
}
