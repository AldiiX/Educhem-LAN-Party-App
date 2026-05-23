"use client"

import {ReactNode, useEffect, useMemo} from "react";
import style from "./layoutclient.module.scss";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {Avatar} from "@/components/Avatar";
import If from "@/components/util/If";
import {Button} from "@/components/Button";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {useWebTheme} from "@/app/_providers/WebThemeProvider";
import {hasRoleAtLeast} from "@/lib/roles";



export default function({ children, appVersion }: { children: ReactNode, appVersion: string }) {
    const pathname = usePathname();
    const router = useRouter();
    const { account: loggedAccount, setAccount } = useAuth();
    const {toggleTheme} = useWebTheme();

    const navItems = useMemo(() => [
        { href: "/app", label: "Home", icon: "/icons/home.svg", exact: true },
        { href: "/app/announcements", label: "Oznámení", icon: "/icons/bell.svg" },
        { href: "/app/map", label: "Mapa", icon: "/icons/map.svg" },
        { href: "/app/reservations", label: "Rezervace", icon: "/icons/calc.svg" },
        ...(loggedAccount !== null ? [{ href: "/app/chat", label: "Chat", icon: "/icons/chat.svg" }] : []),
        { href: "/app/tournaments", label: "Turnaje", icon: "/icons/trophy_star.svg" },
        { href: "/app/problem", label: "Nahlásit problém", icon: "/icons/warn2.svg" },
        ...(hasRoleAtLeast(loggedAccount, "TeacherOrg") ? [{ href: "/app/administration", label: "Administrace", icon: "/icons/user_with_shield.svg" }] : []),
        ...(loggedAccount !== null ? [{ href: "/app/account", label: "Můj účet", icon: "/icons/account.svg", pushBottom: true }] : []),
        ...(loggedAccount !== null ? [{ href: "/app/profile", label: "Veřejný profil", icon: "/icons/account_outline.svg", avatar: true }] : []),
    ], [loggedAccount]);

    const isActive = (href: string, exact?: boolean) => {
        if (href === "/app/profile") {
            return pathname === "/app/profile" || pathname === `/app/profile/${loggedAccount?.id}`;
        }

        if (exact) {
            return pathname === href;
        }

        return pathname === href || pathname.startsWith(`${href}/`);
    };

    const closeMobileMenu = () => {
        const menuToggle = document.getElementById("app-mobile-menu-toggle") as HTMLInputElement | null;
        if (menuToggle) {
            menuToggle.checked = false;
        }
    };

    useEffect(() => {
        const styleId = "minecraft-easter-egg-style";

        (window as any).minecraft = () => {
            if(document.getElementById(styleId)) {
                return;
            }

            const styleElement = document.createElement("style");
            styleElement.id = styleId;
            styleElement.innerHTML = `
            @font-face {
                font-family: "monocraft";
                src: url("/fonts/Monocraft.ttc");
            }

            *, *:before, *:after {
                font-family: "monocraft", sans-serif !important;
                border-radius: 0 !important;
            }
        `;

            document.head.appendChild(styleElement);
            window.open("https://youtu.be/iTRhAlvQjVQ?si=D5wLIenHe3RWw8na&t=28");
            return "chicken jockey!"
        };

        return () => {
            delete (window as any).minecraft;
            document.getElementById(styleId)?.remove();
        };
    }, []);

    const logout = async () => {
        await fetch("/api/v1/account/logout", {method: "POST"});
        setAccount(null);
        router.push("/app/login");
        router.refresh();
    };

    return <div className={style.layout} data-app-layout-shell>
        <div className={style.panel}>
            <Link href="/" className={style.logo}>
                <div></div>
                <p>EDUCHEM<br/>LAN Party</p>
            </Link>

            <nav>
                {navItems.map((item) => (
                    <Link
                        key={item.href}
                        href={item.href}
                        className={isActive(item.href, item.exact) ? style.active : ""}
                        style={item.pushBottom ? { marginTop: "auto" } : undefined}
                    >
                        {item.avatar ? (
                            <Avatar name={loggedAccount?.fullName ?? ""} size="24px" className={style.avatar} src={loggedAccount?.avatarUrl} />
                        ) : (
                            <div className={style.icon} style={{ maskImage: `url(${item.icon})` }}></div>
                        )}
                        <p>{item.label}</p>
                    </Link>
                ))}
            </nav>

            <footer>
                <p>© { new Date().getFullYear() } EDUCHEM LAN Party</p>
                <p>Vytvořili: <a href="https://stanislavskudrna.cz" target="_blank">Stanislav Škudrna</a>, <a href="https://serhii.cz" target="_blank">Serhii Yavorskyi</a></p>
                <p>v{appVersion}</p>
            </footer>
        </div>


        <div className={style.content}>
            <If
                condition={Boolean(loggedAccount?.bannerUrl)}
                as="div"
                className={style.bannerWallpaper}
                style={loggedAccount?.bannerUrl ? {backgroundImage: `url(${loggedAccount.bannerUrl})`} : undefined}
            />
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
                        <Avatar name={loggedAccount?.fullName ?? ""} size="128px" src={loggedAccount?.avatarUrl} className={style.popoverAvatar} />
                        <h2>{ loggedAccount?.fullName }</h2>
                        <p>{ loggedAccount?.email }</p>

                        <div className={style.popoverActions}>
                            <Link href="/app/account">
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

        <input id="app-mobile-menu-toggle" type="checkbox" className={style.mobileMenuToggle} aria-hidden="true" />

        <div className={style.mobileTabs} aria-label="Mobilní navigace">
            <Link href="/app" aria-label="Domů" className={isActive("/app", true) ? style.active : ""}>
                <span className={style.icon} style={{ maskImage: "url(/icons/home.svg)" }}></span>
            </Link>

            <Link href="/app/reservations" aria-label="Rezervace" className={isActive("/app/reservations") ? style.active : ""}>
                <span className={style.icon} style={{ maskImage: "url(/icons/calc.svg)" }}></span>
            </Link>

            <label htmlFor="app-mobile-menu-toggle" aria-label="Otevřít menu" className={style.menuButton}>
                <span className={style.icon} style={{ maskImage: "url(/icons/menu.svg)" }}></span>
            </label>

            <Link href="/app/map" aria-label="Mapa" className={isActive("/app/map") ? style.active : ""}>
                <span className={style.icon} style={{ maskImage: "url(/icons/map.svg)" }}></span>
            </Link>

            <If condition={loggedAccount !== null} fallback={
                <Link href="/app/login" aria-label="Přihlášení" className={isActive("/app/login") ? style.active : ""}>
                    <span className={style.icon} style={{ maskImage: "url(/icons/login.svg)" }}></span>
                </Link>
            }>
                <Link href="/app/account" aria-label="Můj účet" className={isActive("/app/account") ? style.active : ""}>
                    <Avatar name={loggedAccount?.fullName ?? ""} size="22px" className={style.mobileAvatar} src={loggedAccount?.avatarUrl} />
                </Link>
            </If>
        </div>

        <div className={style.mobileMenu}>
            <div className={style.menuHeader}>
                <Link href="/" className={style.logo} onClick={closeMobileMenu}>
                    <div></div>
                    <p>EDUCHEM<br/>LAN Party</p>
                </Link>
            </div>

            <nav>
                {navItems.map((item) => (
                    <Link
                        key={item.href}
                        href={item.href}
                        className={isActive(item.href, item.exact) ? style.active : ""}
                        onClick={closeMobileMenu}
                    >
                        {item.avatar ? (
                            <Avatar name={loggedAccount?.fullName ?? ""} size="28px" className={style.avatar} src={loggedAccount?.avatarUrl} />
                        ) : (
                            <span className={style.icon} style={{ maskImage: `url(${item.icon})` }}></span>
                        )}
                        <span>{item.label}</span>
                    </Link>
                ))}
            </nav>

            <div className={style.menuActions}>
                <button type="button" onClick={toggleTheme}>
                    <span className={style.icon} style={{ maskImage: "url(/icons/theme.svg)" }}></span>
                    <span>Změnit theme</span>
                </button>

                <If condition={loggedAccount !== null}>
                    <button type="button" onClick={logout}>
                        <span className={style.icon} style={{ maskImage: "url(/icons/logout.svg)" }}></span>
                        <span>Odhlásit se</span>
                    </button>
                </If>
            </div>

            <label htmlFor="app-mobile-menu-toggle" aria-label="Zavřít menu" className={style.menuClose}>
                <span className={style.icon} style={{ maskImage: "url(/icons/close.svg)" }}></span>
            </label>
        </div>
    </div>
}
