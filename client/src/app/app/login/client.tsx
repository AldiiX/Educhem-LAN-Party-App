"use client";

import style from "./client.module.scss"
import Link from "next/link";
import {type CSSProperties, useEffect, useState} from "react";
import {useRouter, useSearchParams} from "next/navigation";
import toast from "react-hot-toast";
import useLogin from "@/app/app/login/_hooks/useLogin";
import {Modal} from "@/components/Modal";
import {Button} from "@/components/Button";
import {platforms} from "@/data/platforms";

export default function() {
    const { login, resetPassword } = useLogin();
    const router = useRouter();
    const searchParams = useSearchParams();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [resetEmail, setResetEmail] = useState("");
    const [resetOpen, setResetOpen] = useState(false);
    const [resetLoading, setResetLoading] = useState(false);
    const [loginLoading, setLoginLoading] = useState(false);

    useEffect(() => {
        const socialProvider = (["discord", "github", "google", "steam"] as const).find(provider => searchParams.get(provider) != null);
        if(!socialProvider) return;

        const socialStatus = searchParams.get(socialProvider);
		const platformName = platforms.find(platform => platform.id === socialProvider)?.name ?? socialProvider;
        const message = {
            "not-linked": `${platformName} účet není propojený s Educhem LAN Party účtem. Přihlas se e-mailem a propoj ho v nastavení.`,
            cancelled: `Přihlášení přes ${platformName} bylo zrušeno.`,
            error: `Přihlášení přes ${platformName} se nepodařilo.`,
        }[socialStatus ?? ""];
        if(message) toast.error(message);
        router.replace("/app/login");
    }, [router, searchParams]);

    async function submitLogin() {
        if(loginLoading) return;

        setLoginLoading(true);
        try {
            await login(email, password);
        } finally {
            setLoginLoading(false);
        }
    }

    async function submitResetPassword() {
        setResetLoading(true);
        try {
            const ok = await resetPassword(resetEmail);
            if(ok) setResetOpen(false);
        } finally {
            setResetLoading(false);
        }
    }

    return (
        <div className={style.parent}>
            <div className={style["left-side"]}>
                <Link href="/app" className={style["title"]}>
                    <div className={style["logo"]}></div>
                    <h1>EDUCHEM<br/>LAN Party</h1>
                </Link>

                <form className={style["login-container"]} onSubmit={event => {
                    event.preventDefault();
                    submitLogin().then();
                }}>
                    <div>
                        <p>E-mail</p>
                        <input type="text" placeholder="karel@honsig.eu" onChange={(e) => setEmail(e.currentTarget.value)} />
                    </div>

                    <div>
                        <div className={style.passwordHeader}>
                            <p>Heslo</p>
                            <button type="button" className={style.forgotPassword} onClick={() => {
                                setResetEmail(email);
                                setResetOpen(true);
                            }}>Zapomenuté heslo?</button>
                        </div>
                        <input type="password" placeholder="•••••••" onChange={(e) => setPassword(e.currentTarget.value)} />
                    </div>
                    <Button type="primary" text="Přihlásit se" buttonType="submit" className={style.submitBtn} disabled={loginLoading} loading={loginLoading} />

                    <div className={style.socialLogins}>
                        <p>Jiné možnosti přihlášení</p>
                        <div className={style.socialLoginIcons}>
                            {platforms.map(platform => <button key={platform.id} type="button" className={style.socialLogin} disabled={platform.disabled} aria-label={`Přihlásit se přes ${platform.name}`} title={platform.disabled ? `${platform.name} zatím není dostupný` : `Přihlásit se přes ${platform.name}`} onClick={() => {
                                if(platform.id !== "instagram") window.location.assign(`/api/v1/${platform.id}/login`);
                            }}>
                                <span data-platform={platform.id} style={{
                                    maskImage: `url(${platform.icon})`,
                                    "--platform-icon-background": platform.iconBackground,
                                } as CSSProperties}></span>
                            </button>)}
                        </div>
                    </div>

                </form>
            </div>
            <div className={style["right-side"]}>
                <div className={style["image"]}>

                </div>
                <div className={style["PC-count"]}>

                </div>
            </div>
            <Modal open={resetOpen} onClose={() => setResetOpen(false)} className={style.resetModal}>
                <form onSubmit={event => {
                    event.preventDefault();
                    submitResetPassword();
                }}>
                    <div className={style.modalHeader}>
                        <span className={style.modalIcon}></span>
                        <div>
                            <h2>Obnova hesla</h2>
                            <p>Zadejte e-mail účtu. Pošleme vám odkaz pro nastavení nového hesla.</p>
                        </div>
                    </div>
                    <label>
                        <span>E-mail</span>
                        <div className={style.inputWrap}>
                            <span></span>
                            <input type="email" value={resetEmail} placeholder="karel@honsig.eu" onChange={event => setResetEmail(event.target.value)} />
                        </div>
                    </label>
                    <Button type="primary" text="Odeslat odkaz" buttonType="submit" className={style.resetSubmit} disabled={!resetEmail || resetLoading} loading={resetLoading} />
                </form>
            </Modal>
        </div>
    );
}
