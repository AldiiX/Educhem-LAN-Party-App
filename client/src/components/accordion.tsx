'use client'

import {type ReactNode, useState} from 'react'
import styles from './accordion.module.scss'

interface AccordionItemProps {
    title: string
    children: ReactNode
    defaultOpen?: boolean
}

export function AccordionItem({title, children, defaultOpen = false}: AccordionItemProps) {
    const [open, setOpen] = useState(defaultOpen)

    return (
        <div className={styles.item}>
            <button
                className={styles.trigger}
                onClick={() => setOpen(! open)}
                aria-expanded={open}
            >
                <span>{title}</span>
                <span className={`${styles.chevron}${open ? ` ${styles.open}` : ''}`} aria-hidden="true"/>
            </button>
            <div className={`${styles.content}${open ? '' : ` ${styles.hidden}`}`}>
                {children}
            </div>
        </div>
    )
}

interface AccordionProps {
    children: ReactNode
}

export function Accordion({children}: AccordionProps) {
    return <div className={styles.root}>{children}</div>
}
