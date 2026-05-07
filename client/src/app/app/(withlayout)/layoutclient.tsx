"use client"

import {ReactNode} from "react";
import style from "./layoutclient.module.scss";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {Avatar} from "@/components/Avatar";
import If from "@/components/util/If";
import {Button} from "@/components/Button";


const LOGGED_IN = true;


export default function({ children }: { children: ReactNode }) {
    const pathname = usePathname();

    return <div className={style.layout}>
        <div className={style.panel}>
            <Link href="/" className={style.logo}>
                <div></div>
                <p>EDUCHEM<br/>LAN Party</p>
            </Link>

            <nav>
                <Link href="/app" className={pathname === "/app" ? style.active : ""}>
                    <div className={style.icon} style={{ maskImage: 'url(/icons/home.svg)' }}></div>
                    <p>Home</p>
                </Link>

                <Link href="/app/announcements" className={pathname === "/app/announcements" ? style.active : ""}>
                    <div className={style.icon} style={{ maskImage: 'url(/icons/bell.svg)' }}></div>
                    <p>Oznámení</p>
                </Link>

                <Link href="/app/map" className={pathname === "/app/map" ? style.active : ""}>
                    <div className={style.icon} style={{ maskImage: 'url(/icons/map.svg)' }}></div>
                    <p>Mapa</p>
                </Link>

                <Link href="/app/reservations" className={pathname === "/app/reservations" ? style.active : ""}>
                    <div className={style.icon} style={{ maskImage: 'url(/icons/calc.svg)' }}></div>
                    <p>Rezervace</p>
                </Link>

                <If condition={LOGGED_IN}>
                    <Link href="/app/chat" className={pathname === "/app/chat" ? style.active : ""}>
                        <div className={style.icon} style={{ maskImage: 'url(/icons/chat.svg)' }}></div>
                        <p>Chat</p>
                    </Link>
                </If>

                <Link href="/app/tournaments" className={pathname === "/app/tournaments" ? style.active : ""}>
                    <div className={style.icon} style={{ maskImage: 'url(/icons/trophy_star.svg)' }}></div>
                    <p>Turnaje</p>
                </Link>

                <Link href="/app/problem" className={pathname === "/app/problem" ? style.active : ""}>
                    <div className={style.icon} style={{ maskImage: 'url(/icons/warn2.svg)' }}></div>
                    <p>Nahlásit problém</p>
                </Link>

                <If condition={LOGGED_IN}>
                    <Link href="/app/administration" className={pathname === "/app/administration" ? style.active : ""}>
                        <div className={style.icon} style={{ maskImage: 'url(/icons/user_with_shield.svg)' }}></div>
                        <p>Administrace</p>
                    </Link>
                </If>

                <If condition={LOGGED_IN}>
                    <Link href="/app/profile" className={pathname === "/app/profile" ? style.active : ""} style={{ marginTop: "auto" }}>
                        <Avatar name={"Stanislav Škudrna"} size="24px" className={style.avatar} />
                        <p>Tvůj profil</p>
                    </Link>
                </If>
            </nav>

            <footer>
                <p>© { new Date().getFullYear() } EDUCHEM LAN Party</p>
                <p>Vytvořili: <a href="https://stanislavskudrna.cz" target="_blank">Stanislav Škudrna</a>, <a href="https://serhii.cz" target="_blank">Serhii Yavorskyi</a></p>
                <p>v4.1.0</p>
            </footer>
        </div>

        <div className={style.content}>
            <div className={style.login}>
                <If condition={LOGGED_IN} fallback={
                    // uzivatel neni lognuty
                    <Link href="/app/login">
                        <Button type="primary" text="Přihlásit se" style={{ padding: "10px 32px" }} />
                    </Link>
                }>

                </If>
            </div>

            {children}
        </div>
    </div>
}