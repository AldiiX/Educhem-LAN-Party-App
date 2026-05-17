"use client";

import {useMemo, useState} from "react";
import toast from "react-hot-toast";
import {PasswordForm} from "./types";

type PasswordRouter = {
    push: (href: string) => void;
    refresh: () => void;
};

async function getErrorMessage(response: Response, fallback: string) {
    const text = await response.text();
    return text.trim() || fallback;
}

export function useAccountPassword(setAccount: (account: null) => void, router: PasswordRouter) {
    const [changingPassword, setChangingPassword] = useState(false);
    const [passwordForm, setPasswordForm] = useState<PasswordForm>({
        oldPassword: "",
        newPassword: "",
        newPasswordConfirmation: "",
    });

    const passwordValidations = useMemo(() => ({
        minLength: passwordForm.newPassword.length >= 8,
        lower: /[a-z]/.test(passwordForm.newPassword),
        upper: /[A-Z]/.test(passwordForm.newPassword),
        number: /\d/.test(passwordForm.newPassword),
        special: /[^a-zA-Z0-9]/.test(passwordForm.newPassword),
        differentFromOld: passwordForm.oldPassword.length === 0 || passwordForm.newPassword !== passwordForm.oldPassword,
    }), [passwordForm.newPassword, passwordForm.oldPassword]);

    const canSubmitPassword = Object.values(passwordValidations).every(Boolean)
        && passwordForm.newPassword === passwordForm.newPasswordConfirmation
        && passwordForm.oldPassword.length > 0;

    async function changePassword() {
        if(!canSubmitPassword) return;

        setChangingPassword(true);
        try {
            const response = await fetch("/api/v1/account/me/password", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({
                    oldPassword: passwordForm.oldPassword,
                    newPassword: passwordForm.newPassword,
                }),
            });

            if(!response.ok) {
                toast.error(await getErrorMessage(response, "Heslo se nepodařilo změnit."));
                return;
            }

            toast.success("Heslo změněno. Přihlas se prosím znovu.");
            setAccount(null);
            router.push("/app/login");
            router.refresh();
        } finally {
            setChangingPassword(false);
        }
    }

    return {
        passwordForm,
        setPasswordForm,
        passwordValidations,
        canSubmitPassword,
        changingPassword,
        changePassword,
    };
}
