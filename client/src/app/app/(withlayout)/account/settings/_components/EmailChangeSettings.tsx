"use client";

import {useEffect, useState} from "react";
import useSWR from "swr";
import toast from "react-hot-toast";
import {Button} from "@/components/Button";
import {Modal} from "@/components/Modal";
import {ModalDestructive} from "@/components/ModalDialog";
import {Account} from "@/schemas/AccountSchema";
import {phrase} from "@/lib/communicationStyle";
import {apiFetch} from "@/lib/apiClient";
import {emailChangeDate, emailChangeRequest} from "@/lib/emailChange";
import styles from "./EmailChangeSettings.module.scss";

export function EmailChangeSettings({account}: {account: Account}) {
    const {data, error, isLoading, mutate} = useSWR(["email-change", account.id], () => emailChangeRequest(), {
        refreshInterval: 15000, shouldRetryOnError: false,
    });
    const [open, setOpen] = useState(false);
    const [cancelOpen, setCancelOpen] = useState(false);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [busy, setBusy] = useState(false);
    const [message, setMessage] = useState("");
    const [now, setNow] = useState(() => Date.now());
    const request = data?.request;
    const pending = request?.state === "pending" && new Date(request.expiresAtUtc).getTime() > now;
    const cooldown = request?.resendAtUtc ? Math.max(0, Math.ceil((new Date(request.resendAtUtc).getTime() - now) / 1000)) : 0;
    const style = account.communicationStyle;

    useEffect(() => {
        const interval = window.setInterval(() => setNow(Date.now()), 1000);
        return () => window.clearInterval(interval);
    }, []);

    function close() {
        if (busy) return;
        setOpen(false);
        setPassword("");
        setMessage("");
    }

    async function submit(path: string, body: object) {
        if (busy) return;
        setBusy(true);
        setMessage("");
        try {
            const result = await emailChangeRequest(path, body);
            await mutate(result, {revalidate: false});
            if (!result.emailsSent) toast.error("Žádost je uložená, ale některý e-mail se nepodařilo odeslat. Použijte Poslat znovu.");
            else toast.success(path === "/cancel" ? "Žádost byla zrušena." : "Potvrzení bylo odesláno. Do dokončení platí původní e-mail.");
            setPassword("");
            setOpen(false);
            setCancelOpen(false);
        } catch (reason) {
            const text = reason instanceof Error ? reason.message : "Požadavek se nepodařilo dokončit.";
            setMessage(text);
            if (!open) toast.error(text);
            await mutate();
        } finally { setBusy(false); }
    }

    async function resetPassword() {
        if (busy) return;
        setBusy(true);
        try {
            const response = await apiFetch("/api/v1/account/forgot-password", {
                method: "POST", headers: {"Content-Type": "application/json"},
                body: JSON.stringify({email: account.email}),
            });
            if (!response.ok) throw new Error();
            toast.success("Pokud je možné heslo obnovit, přijde odkaz na současný e-mail účtu.");
        } catch { toast.error("Obnovu hesla se nepodařilo odeslat."); }
        finally { setBusy(false); }
    }

    return <div className={styles.emailSetting}>
        <label><span>E-mail</span><input type="email" value={account.email ?? ""} disabled /></label>
        <Button type="secondary" text={pending ? "Zadat jinou adresu" : "Změnit e-mail"} disabled={busy || isLoading || !!error} onClick={() => {
            setEmail(""); setPassword(""); setMessage(""); setOpen(true);
        }} />
        {isLoading && <p role="status">Načítání změny e-mailu…</p>}
        {error && <div role="alert"><p>{error.message}</p><Button type="secondary" text="Zkusit znovu" onClick={() => { void mutate(); }} /></div>}
        {request && request.state !== "completed" && <div className={styles.pending} aria-live="polite">
            {pending ? <>
                <div className={styles.pendingHeader}>
                    <span>Čeká na potvrzení</span>
                    <span className={styles.progress}>{Number(request.oldConfirmed) + Number(request.newConfirmed)} ze 2</span>
                </div>
                <div className={styles.destination}>
                    <span>Nový přihlašovací e-mail</span>
                    <strong>{request.newEmail}</strong>
                </div>
                <ul className={styles.confirmations}>
                    {[
                        {label: "Původní adresa", confirmed: request.oldConfirmed},
                        {label: "Nová adresa", confirmed: request.newConfirmed},
                    ].map(address => <li key={address.label}>
                        <span>{address.label}</span>
                        <strong className={address.confirmed ? styles.confirmed : styles.waiting}>
                            <span aria-hidden="true">{address.confirmed ? "✓" : "○"}</span>
                            {address.confirmed ? "Potvrzeno" : "Čeká"}
                        </strong>
                    </li>)}
                </ul>
                <div className={styles.pendingDetails}>
                    <p>Platnost do <time dateTime={request.expiresAtUtc}>{emailChangeDate(request.expiresAtUtc)}</time> (Praha)</p>
                    <p>Do dokončení platí původní přihlašovací e-mail.</p>
                </div>
                <div className={styles.actions}>
                    <Button type="secondary" text={cooldown > 0 ? `Poslat znovu za ${cooldown} s` : "Poslat znovu"} disabled={busy || cooldown > 0} onClick={() => { void submit("/resend", {}); }} />
                    <Button type="tertiary" text="Zrušit žádost" disabled={busy} onClick={() => setCancelOpen(true)} />
                </div>
            </> : <p>{request.state === "cancelled" ? "Poslední žádost byla zrušena." : "Platnost žádosti vypršela. E-mail zůstal beze změny."}</p>}
        </div>}
        <Modal open={open} onClose={close} closeOnBackdrop={!busy} className={styles.emailModal}>
            <form onSubmit={event => { event.preventDefault(); event.stopPropagation(); void submit("", {email, password}); }}>
                <header className={styles.modalHeader}>
                    <span className={styles.emailIcon} aria-hidden="true" />
                    <div><p>Přihlašovací údaje</p><h2>Změna e-mailu</h2></div>
                </header>
                <div className={styles.instructions}>
                    <p>Na původní i novou adresu pošleme potvrzovací odkaz.</p>
                    <p>{phrase(style, "Obě adresy potvrď do 30 minut. Heslo zůstane stejné.", "Obě adresy potvrďte do 30 minut. Heslo zůstane stejné.")}</p>
                </div>
                {pending && <div className={styles.notice}>
                    <strong>Nová žádost nahradí předchozí</strong>
                    <p>Původní odkazy přestanou platit. Obě adresy bude potřeba potvrdit znovu.</p>
                </div>}
                <div className={styles.fields}>
                    <label><span>Nový e-mail</span><input type="email" autoComplete="email" placeholder="např. jmeno@example.cz" maxLength={96} required value={email} onChange={event => setEmail(event.target.value)} disabled={busy} /></label>
                    <div className={styles.passwordField}>
                        <label><span>Současné heslo</span><input type="password" autoComplete="current-password" maxLength={512} required value={password} onChange={event => setPassword(event.target.value)} disabled={busy} /></label>
                        <button type="button" className={styles.resetLink} disabled={busy} onClick={() => { void resetPassword(); }}>Zapomenuté heslo?</button>
                    </div>
                </div>
                {message && <p role="alert" className={styles.error}>{message}</p>}
                <footer className={styles.modalFooter}>
                    <p>{phrase(style, "Nemáš přístup k původnímu e-mailu? Kontaktuj administrátora.", "Nemáte přístup k původnímu e-mailu? Kontaktujte administrátora.")}</p>
                    <div className={styles.actions}>
                        <Button type="tertiary" text="Zrušit" disabled={busy} onClick={close} />
                        <Button type="primary" text="Odeslat potvrzení" buttonType="submit" disabled={busy || !email.trim() || !password} loading={busy} />
                    </div>
                </footer>
            </form>
        </Modal>
        <ModalDestructive open={cancelOpen} title="Zrušit změnu e-mailu" description="Přihlašovací e-mail zůstane beze změny. Potvrzovací odkazy přestanou platit." confirmText="Zrušit žádost" cancelText="Zpět" loading={busy} onClose={() => { if (!busy) setCancelOpen(false); }} onConfirm={() => submit("/cancel", {})} />
    </div>;
}
