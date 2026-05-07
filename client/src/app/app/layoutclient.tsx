"use client"

import {ReactNode} from "react";
import style from "./layoutclient.module.scss";

export default function({ children }: { children: ReactNode }) {
    return <div className={style.layout}>
        <div className={style.panel}>
            <p>Home</p>
            <p>Rezervace</p>
            <p>Chat</p>
            <p>Administrace</p>
        </div>

        <div className={style.content}>
            {children}
        </div>
    </div>
}