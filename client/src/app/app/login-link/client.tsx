"use client";

import Link from "next/link";
import {useEffect, useState} from "react";
import {Button} from "@/components/Button";
import {apiFetch} from "@/lib/apiClient";
import {useOneTimeToken} from "@/hooks/useOneTimeToken";
import layout from "../reset-password/client.module.scss";
import styles from "./client.module.scss";

export default function LoginLinkClient() {
    const token = useOneTimeToken();
    const [email, setEmail] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!token) return;
        let active = true;
        setLoading(true);
        apiFetch("/api/v1/account/login-link/preview", {
            method: "POST",
            cache: "no-store",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({token}),
        }).then(async response => {
            if (!response.ok) throw new Error("Odkaz je neplatný nebo vypršel.");
            const result: {email: string} = await response.json();
            if (active) setEmail(result.email);
        }).catch(reason => {
            if (active) setError(reason instanceof Error ? reason.message : "Odkaz nelze ověřit.");
        }).finally(() => {
            if (active) setLoading(false);
        });
        return () => { active = false; };
    }, [token]);

    async function confirm() {
        if (!token || !email || loading) return;
        setLoading(true);
        setError("");
        try {
            const response = await apiFetch("/api/v1/account/login-link", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({token}),
            });
            if (!response.ok) {
                setError("Odkaz je neplatný nebo vypršel. Přihlaste se e-mailem a heslem.");
                setLoading(false);
                return;
            }
            window.location.replace("/app");
        } catch {
            setError("Přihlášení se nepodařilo. Zkuste to znovu.");
            setLoading(false);
        }
    }

    return <LoginLinkView email={email} error={token === "" ? "Odkaz není platný. Otevřete odkaz z e-mailu." : error} loading={loading} onConfirm={() => { void confirm(); }} />;
}

export function LoginLinkView({email, error, loading, onConfirm}: {
    email: string;
    error: string;
    loading: boolean;
    onConfirm: () => void;
}) {
    return <main className={`${layout.page} ${styles.page}`}>
        <Link href="/app" className={`${layout.logo} ${styles.logo}`}>
            <span aria-hidden="true"></span>
            <strong>EDUCHEM<br/>LAN Party</strong>
        </Link>
        <section className={styles.card} aria-busy={loading || (!email && !error)} aria-labelledby="login-link-title">
            <header className={styles.header}>
                <span className={styles.loginIcon} aria-hidden="true" />
                <h1 id="login-link-title">Potvrzení přihlášení</h1>
                <p>Zkontrolujte účet, ke kterému se přihlašujete.</p>
            </header>
            {error && <p className={styles.error} role="alert">{error}</p>}
            {email ? <>
                <div className={styles.account}>
                    <span className={styles.accountIcon} aria-hidden="true" />
                    <div className={styles.accountDetails}>
                        <span>Přihlašujete se k účtu</span>
                        <strong>{email}</strong>
                    </div>
                </div>
                <p className={styles.notice}>Pokračujte pouze, pokud je to váš účet.<br/>Případné současné přihlášení bude nahrazeno.</p>
            </> : !error && <p className={styles.status} role="status">Ověřování odkazu…</p>}
            <div className={styles.actions}>
                <Button type="primary" text="Potvrdit přihlášení" className={styles.confirmButton} disabled={!email || loading} loading={loading} onClick={onConfirm} />
                <Link href="/app/login" className={styles.backLink}>
                    <span aria-hidden="true" />Zpět na přihlášení
                </Link>
            </div>
        </section>
    </main>;
}
