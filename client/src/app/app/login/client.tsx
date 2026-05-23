"use client";

import style from "./client.module.scss"
import Link from "next/link";
import { useState } from "react";
import useLogin from "@/app/app/login/_hooks/useLogin";
import {Modal} from "@/components/Modal";
import {Button} from "@/components/Button";

export default function() {
    const { login, resetPassword } = useLogin();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [resetEmail, setResetEmail] = useState("");
    const [resetOpen, setResetOpen] = useState(false);
    const [resetLoading, setResetLoading] = useState(false);
    const [loginLoading, setLoginLoading] = useState(false);

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
                        <p>Heslo</p>
                        <input type="password" placeholder="•••••••" onChange={(e) => setPassword(e.currentTarget.value)} />
                    </div>
                    <Button type="primary" text="Přihlásit se" buttonType="submit" className={style.submitBtn} disabled={loginLoading} loading={loginLoading} />

                    <button type="button" className={style.forgotPassword} onClick={() => {
                        setResetEmail(email);
                        setResetOpen(true);
                    }}>Zapoměl jsem heslo</button>

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
                            <p>Zadej e-mail účtu. Pošleme ti odkaz pro nastavení nového hesla.</p>
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
