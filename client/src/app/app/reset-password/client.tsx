"use client";

import Link from "next/link";
import {useRouter} from "next/navigation";
import {useMemo, useState} from "react";
import toast from "react-hot-toast";
import {Button} from "@/components/Button";
import styles from "./client.module.scss";

export default function ResetPasswordClient({token}: {token: string}) {
    const router = useRouter();
    const [password, setPassword] = useState("");
    const [passwordConfirmation, setPasswordConfirmation] = useState("");
    const [loading, setLoading] = useState(false);

    const passwordValidations = useMemo(() => ({
        minLength: password.length >= 8,
        lower: /[a-z]/.test(password),
        upper: /[A-Z]/.test(password),
        number: /\d/.test(password),
        special: /[^a-zA-Z0-9]/.test(password),
    }), [password]);

    const canSubmit = token.length > 0
        && password === passwordConfirmation
        && Object.values(passwordValidations).every(Boolean);

    async function submitReset() {
        if(!canSubmit) return;

        setLoading(true);
        try {
            const response = await fetch("/api/v1/account/reset-password", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({
                    token,
                    newPassword: password,
                }),
            });

            if(!response.ok) {
                toast.error("Odkaz je neplatný nebo vypršel.");
                return;
            }

            toast.success("Heslo bylo změněno. Teď se můžeš přihlásit.");
            router.push("/app/login");
        } finally {
            setLoading(false);
        }
    }

    return <main className={styles.page}>
        <Link href="/app" className={styles.logo}>
            <span></span>
            <strong>EDUCHEM<br/>LAN Party</strong>
        </Link>

        <form className={styles.card} onSubmit={event => {
            event.preventDefault();
            submitReset();
        }}>
            <h1>Reset hesla</h1>
            {!token && <p className={styles.error}>Resetovací odkaz není platný.</p>}

            <label>
                <span>Nové heslo</span>
                <input type="password" autoComplete="new-password" value={password} onChange={event => setPassword(event.target.value)} />
            </label>
            <label>
                <span>Nové heslo potvrzení</span>
                <input type="password" autoComplete="new-password" value={passwordConfirmation} onChange={event => setPasswordConfirmation(event.target.value)} />
            </label>

            {password.length > 0 && <div className={styles.rules}>
                <p className={passwordValidations.minLength ? styles.valid : ""}>Alespoň 8 znaků</p>
                <p className={passwordValidations.lower ? styles.valid : ""}>Alespoň 1 malé písmeno</p>
                <p className={passwordValidations.upper ? styles.valid : ""}>Alespoň 1 velké písmeno</p>
                <p className={passwordValidations.number ? styles.valid : ""}>Alespoň 1 číslo</p>
                <p className={passwordValidations.special ? styles.valid : ""}>Alespoň 1 speciální znak</p>
            </div>}

            <Button type="primary" text="Nastavit nové heslo" buttonType="submit" disabled={!canSubmit || loading} loading={loading} />
            <Link href="/app/login" className={styles.backLink}>Zpět na přihlášení</Link>
        </form>
    </main>;
}
