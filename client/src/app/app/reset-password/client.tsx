"use client";

import Link from "next/link";
import {useRouter} from "next/navigation";
import {useEffect, useMemo, useState} from "react";
import toast from "react-hot-toast";
import {Button} from "@/components/Button";
import styles from "./client.module.scss";
import {apiFetch} from "@/lib/apiClient";
import {useOneTimeToken} from "@/hooks/useOneTimeToken";

export default function ResetPasswordClient() {
    const token = useOneTimeToken();
    const router = useRouter();
    const [email, setEmail] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [password, setPassword] = useState("");
    const [passwordConfirmation, setPasswordConfirmation] = useState("");

    useEffect(() => {
        if (token === null) return;
        if (!token) {
            setError("Resetovací odkaz není platný.");
            setLoading(false);
            return;
        }

        let active = true;
        setLoading(true);
        apiFetch("/api/v1/account/reset-password/preview", {
            method: "POST",
            cache: "no-store",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({token}),
        }).then(async response => {
            if (!response.ok) {
                throw new Error("Resetovací odkaz není platný nebo vypršel.");
            }
            const result: {email: string} = await response.json();
            if (active) {
                setEmail(result.email);
                setLoading(false);
            }
        }).catch(reason => {
            if (active) {
                setError(reason instanceof Error ? reason.message : "Resetovací odkaz není platný nebo vypršel.");
                setLoading(false);
            }
        });

        return () => { active = false; };
    }, [token]);

    const passwordValidations = useMemo(() => ({
        minLength: password.length >= 8,
        lower: /[a-z]/.test(password),
        upper: /[A-Z]/.test(password),
        number: /\d/.test(password),
        special: /[^a-zA-Z0-9]/.test(password),
    }), [password]);

    const canSubmit = !!token
        && password === passwordConfirmation
        && Object.values(passwordValidations).every(Boolean);

    async function submitReset() {
        if (!canSubmit || submitting) return;

        setSubmitting(true);
        try {
            const response = await apiFetch("/api/v1/account/reset-password", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({
                    token,
                    newPassword: password,
                }),
            });

            if (!response.ok) {
                const message = (await response.text().catch(() => "")).trim();
                const fallback = "Odkaz je neplatný nebo vypršel.";
                toast.error(message || fallback);
                setError(message || fallback);
                return;
            }

            toast.success("Heslo bylo změněno. Teď se můžete přihlásit.");
            router.push("/app/login");
        } catch {
            toast.error("Nastavení hesla se nezdařilo. Zkuste to znovu.");
        } finally {
            setSubmitting(false);
        }
    }

    const isInvalid = !loading && (error !== "" || token === "");

    return (
        <main className={styles.page}>
            <Link href="/app" className={styles.logo}>
                <span aria-hidden="true"></span>
                <strong>EDUCHEM<br />LAN Party</strong>
            </Link>

            <section className={styles.card} aria-busy={loading} aria-labelledby="reset-password-title">
                <header className={styles.header}>
                    <span className={styles.resetIcon} aria-hidden="true" />
                    <h1 id="reset-password-title">Reset hesla</h1>
                    <p>
                        {isInvalid
                            ? "Resetovací odkaz nelze použít."
                            : "Zvolte si nové heslo k vašemu účtu."}
                    </p>
                </header>

                {loading && <p className={styles.status} role="status">Ověřování odkazu…</p>}

                {isInvalid ? (
                    <>
                        <p className={styles.error} role="alert">
                            {error || "Resetovací odkaz není platný."}
                        </p>
                        <p className={styles.notice}>
                            Tento odkaz pro obnovu hesla není platný nebo již vypršel.<br />
                            Na přihlašovací stránce můžete požádat o nový odkaz.
                        </p>
                        <div className={styles.actions}>
                            <Link href="/app/login" className={styles.backLink}>
                                <span aria-hidden="true" />Zpět na přihlášení
                            </Link>
                        </div>
                    </>
                ) : !loading && (
                    <form
                        className={styles.form}
                        onSubmit={event => {
                            event.preventDefault();
                            void submitReset();
                        }}
                    >
                        {email && (
                            <div className={styles.account}>
                                <span className={styles.accountIcon} aria-hidden="true" />
                                <div className={styles.accountDetails}>
                                    <span>Nastavujete nové heslo pro účet</span>
                                    <strong>{email}</strong>
                                </div>
                            </div>
                        )}

                        <div className={styles.fields}>
                            <label>
                                <span>Nové heslo</span>
                                <input
                                    type="password"
                                    autoComplete="new-password"
                                    value={password}
                                    placeholder="••••••••••••"
                                    onChange={event => setPassword(event.target.value)}
                                />
                            </label>
                            <label>
                                <span>Nové heslo potvrzení</span>
                                <input
                                    type="password"
                                    autoComplete="new-password"
                                    value={passwordConfirmation}
                                    placeholder="••••••••••••"
                                    onChange={event => setPasswordConfirmation(event.target.value)}
                                />
                            </label>
                        </div>

                        {password.length > 0 && (
                            <div className={styles.rules}>
                                <p className={passwordValidations.minLength ? styles.valid : ""}>Alespoň 8 znaků</p>
                                <p className={passwordValidations.lower ? styles.valid : ""}>Alespoň 1 malé písmeno</p>
                                <p className={passwordValidations.upper ? styles.valid : ""}>Alespoň 1 velké písmeno</p>
                                <p className={passwordValidations.number ? styles.valid : ""}>Alespoň 1 číslo</p>
                                <p className={passwordValidations.special ? styles.valid : ""}>Alespoň 1 speciální znak</p>
                            </div>
                        )}

                        <div className={styles.actions}>
                            <Button
                                type="primary"
                                text="Nastavit nové heslo"
                                buttonType="submit"
                                className={styles.submitButton}
                                disabled={!canSubmit || submitting}
                                loading={submitting}
                            />
                            <Link href="/app/login" className={styles.backLink}>
                                <span aria-hidden="true" />Zpět na přihlášení
                            </Link>
                        </div>
                    </form>
                )}
            </section>
        </main>
    );
}
