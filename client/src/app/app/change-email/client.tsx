"use client";

import Link from "next/link";
import {useEffect, useRef, useState} from "react";
import {Button} from "@/components/Button";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {phrase} from "@/lib/communicationStyle";
import {emailChangeDate, emailChangeRequest, EmailChangeStatus} from "@/lib/emailChange";
import styles from "./client.module.scss";
import {takeOneTimeTokenFromUrl} from "@/lib/oneTimeToken";

export default function ChangeEmailClient() {
    const {setAccount} = useAuth();
    const token = useRef("");
    const [request, setRequest] = useState<EmailChangeStatus | null>(null);
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(true);
    const [finished, setFinished] = useState(false);
    const [isCancel, setIsCancel] = useState(false);
    const confirmedCount = Number(request?.oldConfirmed ?? false) + Number(request?.newConfirmed ?? false);
    const currentAddress = token.current.split(".")[1];

    useEffect(() => {
        token.current ||= takeOneTimeTokenFromUrl();
        setIsCancel(token.current.split(".")[1] === "cancel");
        let active = true;
        if (!token.current) { setError("Odkaz je neplatný. Otevřete odkaz z e-mailu."); setLoading(false); return; }
        emailChangeRequest("/preview", {token: token.current})
            .then(result => { if (active) setRequest(result.request); })
            .catch(reason => { if (active) setError(reason instanceof Error ? reason.message : "Odkaz nelze ověřit."); })
            .finally(() => { if (active) setLoading(false); });
        return () => { active = false; };
    }, []);

    async function confirm() {
        if (loading || finished) return;
        setLoading(true);
        setError("");
        try {
            const result = await emailChangeRequest("/confirm", {token: token.current});
            setRequest(result.request);
            setFinished(true);
            token.current = "";
            if (result.request?.state === "completed") {
                setAccount(null);
                window.location.replace("/app/login?email-changed=1");
            }
        } catch (reason) { setError(reason instanceof Error ? reason.message : "Změnu se nepodařilo potvrdit."); }
        finally { setLoading(false); }
    }

    return <main className={styles.page}>
        <Link href="/app" className={styles.logo}>
            <span aria-hidden="true"></span>
            <strong>EDUCHEM<br/>LAN Party</strong>
        </Link>
        <section className={styles.card} aria-busy={loading}>
            <header className={styles.header}>
                <span className={styles.emailIcon} aria-hidden="true" />
                <div>
                    <p className={styles.eyebrow}>Přihlašovací údaje</p>
                    <h1>{isCancel ? "Zrušení změny e-mailu" : "Potvrzení změny e-mailu"}</h1>
                </div>
            </header>
            {loading && <p className={styles.loading} role="status">Ověřování…</p>}
            {error && <p className={styles.error} role="alert">{error}</p>}
            {request && <>
                <div className={styles.addresses}>
                    <div className={styles.progress}>
                        <span>Potvrzení adres</span>
                        <strong>{confirmedCount} ze 2</strong>
                    </div>
                    <dl>
                        {[
                            {kind: "old", label: "Původní e-mail", email: request.oldEmail, confirmed: request.oldConfirmed},
                            {kind: "new", label: "Nový e-mail", email: request.newEmail, confirmed: request.newConfirmed},
                        ].map(address => <div key={address.kind} className={`${styles.address} ${currentAddress === address.kind && !finished ? styles.current : ""}`}>
                            <dt>{address.label}{currentAddress === address.kind && !finished && <span className={styles.currentLabel}>Tento odkaz</span>}</dt>
                            <dd>
                                <strong>{address.email}</strong>
                                <span className={`${styles.status} ${address.confirmed ? styles.confirmed : styles.waiting}`}>
                                    <span aria-hidden="true">{address.confirmed ? "✓" : "○"}</span>
                                    {address.confirmed ? "Potvrzeno" : "Čeká na potvrzení"}
                                </span>
                            </dd>
                        </div>)}
                    </dl>
                </div>
                {finished ? <p className={styles.result} role="status">{request.state === "cancelled"
                    ? "Žádost byla zrušena. Přihlašovací e-mail se nezměnil."
                    : "Tato adresa je potvrzená. Změnu dokončí potvrzení druhé adresy. Do té doby platí původní e-mail."}</p> : <>
                    <div className={styles.action}>
                        <p>{isCancel ? "Zrušením žádosti zůstane původní e-mail a všechny potvrzovací odkazy přestanou platit." : phrase(request.communicationStyle,
                            "Potvrď adresu označenou výše. E-mail se změní až po potvrzení obou adres.",
                            "Potvrďte adresu označenou výše. E-mail se změní až po potvrzení obou adres.")}</p>
                        <Button type="primary" text={isCancel ? "Zrušit žádost" : "Potvrdit tuto adresu"} disabled={loading} loading={loading} onClick={() => { void confirm(); }} />
                        <p className={styles.expiry}>Platnost do <time dateTime={request.expiresAtUtc}>{emailChangeDate(request.expiresAtUtc)}</time> (Praha)</p>
                    </div>
                </>}
            </>}
            <footer className={styles.footer}>
                {request && !isCancel && !finished && <p>Po dokončení se ostatní zařízení odhlásí do 10 minut.</p>}
                <p>{phrase(request?.communicationStyle, "Nemáš přístup k původnímu e-mailu? Kontaktuj administrátora.", "Nemáte přístup k původnímu e-mailu? Kontaktujte administrátora.")}</p>
                <Link href="/app/login" className={styles.backLink}>Zpět na přihlášení</Link>
            </footer>
        </section>
    </main>;
}
