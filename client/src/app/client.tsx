"use client";

import styles from "./page.module.scss";
import {useWebTheme} from "@/app/_providers/WebThemeProvider";
import Link from 'next/link'

export default function() {
    const { toggleTheme } = useWebTheme();

    return (
        <div className={styles.page}>
            <div className={styles.center}>
                <div className={styles.logo}></div>
                <h1>This is an ASP.NET + Next.js template</h1>
                <button onClick={toggleTheme} className={styles.btn}>Switch theme</button>
                <Link href={"/about"}>About</Link>
            </div>
        </div>
    );
}