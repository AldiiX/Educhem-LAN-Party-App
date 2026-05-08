"use client"

import {ReactNode} from "react";
import style from "./layoutclient.module.scss";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {Avatar} from "@/components/Avatar";
import If from "@/components/util/If";
import {Button} from "@/components/Button";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {useWebTheme} from "@/app/_providers/WebThemeProvider";



export default function({ children }: { children: ReactNode }) {
    const pathname = usePathname();
    const router = useRouter();
    const { account: loggedAccount, setAccount } = useAuth();
    const {toggleTheme} = useWebTheme();

    const logout = async () => {
        await fetch("/api/v1/account/logout", {method: "POST"});
        setAccount(null);
        router.push("/app/login");
        router.refresh();
    };

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

                <If condition={loggedAccount !== null}>
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

                <If condition={loggedAccount !== null  && (loggedAccount.accountType === "TeacherOrg" || loggedAccount.accountType === "Admin" || loggedAccount.accountType === "SuperAdmin")}>
                    <Link href="/app/administration" className={pathname === "/app/administration" ? style.active : ""}>
                        <div className={style.icon} style={{ maskImage: 'url(/icons/user_with_shield.svg)' }}></div>
                        <p>Administrace</p>
                    </Link>
                </If>

                <If condition={loggedAccount !== null}>
                    <Link href="/app/account" className={pathname === "/app/account" ? style.active : ""} style={{ marginTop: "auto" }}>
                        <div className={style.icon} style={{ maskImage: 'url(/icons/account.svg)' }}></div>
                        <p>Můj účet</p>
                    </Link>
                </If>

                <If condition={loggedAccount !== null}>
                    <Link href="/app/profile" className={pathname === "/app/profile" ? style.active : ""}>
                        <Avatar name={loggedAccount?.fullName ?? ""} size="24px" className={style.avatar} src={loggedAccount?.avatarUrl} />
                        <p>Veřejný profil</p>
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
            <div className={style.loginShell}>
                <div className={style.login}>
                    <If condition={loggedAccount !== null} fallback={
                        // uzivatel neni lognuty
                        <Link href="/app/login">
                            <Button type="primary" text="Přihlásit se" style={{ padding: "10px 32px" }} />
                        </Link>
                    }>
                        <div>
                            <p>Přihlášen{loggedAccount?.gender === "Female" ? "a" : ''} jako</p>
                            <h2>{ loggedAccount?.fullName }</h2>
                        </div>
                        <Avatar name={loggedAccount?.fullName ?? ""} size="48px" src={loggedAccount?.avatarUrl} />
                    </If>
                </div>

                <If condition={loggedAccount !== null}>
                    <div className={style.accountPopover}>
                        <Avatar name={loggedAccount?.fullName ?? ""} size="160px" src={loggedAccount?.avatarUrl} className={style.popoverAvatar} />
                        <h2>{ loggedAccount?.fullName }</h2>
                        <p>{ loggedAccount?.email }</p>

                        <div className={style.popoverActions}>
                            <Link href="/app/profile">
                                <span className={style.actionIcon} style={{ maskImage: "url(/icons/account_outline.svg)" }}></span>
                                <span>Můj účet</span>
                            </Link>
                            <button type="button" onClick={toggleTheme}>
                                <span className={style.actionIcon} style={{ maskImage: "url(/icons/theme.svg)" }}></span>
                                <span>Změnit theme</span>
                            </button>
                        </div>

                        <button type="button" className={style.logoutButton} onClick={logout}>
                            <span className={style.actionIcon} style={{ maskImage: "url(/icons/logout.svg)" }}></span>
                            <span>Odhlásit se</span>
                        </button>
                    </div>
                </If>
            </div>

            {children}
        </div>
    </div>
}
