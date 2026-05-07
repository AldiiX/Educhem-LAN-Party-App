"use client";

import style from "./client.module.scss"
import Link from "next/link";

export default function() {
    return (
        <div className={style.parent}>
            <div className={style["left-side"]}>
                <Link href="/app" className={style["title"]}>
                    <div className={style["logo"]}></div>
                    <h1>EDUCHEM<br/>LAN Party</h1>
                </Link>

                <div className={style["login-container"]}>
                    <div>
                        <p>E-mail</p>
                        <input type="text" placeholder="karel@honsig.eu" />
                    </div>

                    <div>
                        <p>Heslo</p>
                        <input type="password" placeholder="•••••••" />
                    </div>

                    <input type="submit" className={style.submitBtn} value="Přihlásit se" />
                </div>
            </div>
            <div className={style["right-side"]}>
                <div className={style["image"]}>

                </div>
                <div className={style["PC-count"]}>

                </div>
            </div>
        </div>
    );
}