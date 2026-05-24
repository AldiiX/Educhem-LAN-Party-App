import style from "./not-found.module.scss"
import { Metadata } from "next";

export const metadata: Metadata = {
    title: "404 - Stránka nenalezena"
}

export default function NotFound() {
    return (
        <div className={style.errordiv}>
            <div className={style.errorNadpis}>
                <h1>4</h1>
                <div className={style.logo}></div>
                <h1>4</h1>
            </div>

            <h2>Stránka nenalezena</h2>
            <p>Omlouváme se, ale stránka, kterou hledáš, nebyla nalezena.</p>
        </div>
    )
}