"use client"

import style from "./error.module.scss"
import Link from "next/link";

export default function NotFound() {
    return (
        <div className={style.errordiv}>
            <div className={style.errorNadpis}>
                <h1>4</h1>
                <div className={style.logo}></div>
                <h1>4</h1>
            </div>

            <h2>Stránka nenalezena</h2>
            <p>Omlouváme se, ale stránka, kterou hledáte, nebyla nalezena.</p>
        </div>
    )
}