'use client'

import {useEffect, useState} from 'react'
import styles from './back-to-top.module.scss'

export function BackToTop() {
    const [visible, setVisible] = useState(false)

    useEffect(() => {
        function onScroll() {
            setVisible(window.scrollY > 400)
        }

        window.addEventListener('scroll', onScroll, {passive: true})
        return () => window.removeEventListener('scroll', onScroll)
    }, [])

    return (
        <button
            className={`${styles.button}${visible ? ` ${styles.visible}` : ''}`}
            onClick={() => window.scrollTo({top: 0, behavior: 'smooth'})}
            aria-label="Zpět nahoru"
        >
            <span className={styles.arrow} aria-hidden="true"/>
        </button>
    )
}
