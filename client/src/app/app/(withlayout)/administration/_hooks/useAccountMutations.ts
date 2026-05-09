import {Dispatch, FormEvent, SetStateAction} from "react";
import {toast} from "react-hot-toast";
import {Account, AccountSchema} from "@/schemas/AccountSchema";
import {canManageAccountRole} from "@/lib/roles";
import {splitDisplayName} from "./accountForm";
import {AccountForm, ModalMode} from "./types";

type UseAccountMutationsOptions = {
    canManageSelectedAccount: boolean;
    closeModal: () => void;
    form: AccountForm;
    loggedAccount: Account | null;
    modalMode: ModalMode | null;
    refreshAccounts: () => Promise<Account[]>;
    saving: boolean;
    selectedAccount: Account | null;
    setLoggedAccount: (account: Account | null) => void;
    setModalMode: Dispatch<SetStateAction<ModalMode | null>>;
    setSaving: Dispatch<SetStateAction<boolean>>;
    setSelectedAccount: Dispatch<SetStateAction<Account | null>>;
};

export function useAccountMutations({
    canManageSelectedAccount,
    closeModal,
    form,
    loggedAccount,
    modalMode,
    refreshAccounts,
    saving,
    selectedAccount,
    setLoggedAccount,
    setModalMode,
    setSaving,
    setSelectedAccount,
}: UseAccountMutationsOptions) {
    const submitAccount = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if(saving) return;

        if(!canManageAccountRole(loggedAccount, form.accountType)) {
            toast.error("Nemůžeš vytvořit nebo nastavit uživatele se stejnou nebo vyšší rolí.");
            return;
        }

        if(modalMode === "edit" && !canManageSelectedAccount) {
            toast.error("Nemůžeš upravit uživatele se stejnou nebo vyšší rolí.");
            return;
        }

        const {firstName, lastName} = splitDisplayName(form.displayName);

        if(firstName.length < 2 || lastName.length < 2) {
            toast.error("Jméno i příjmení musí mít alespoň 2 znaky.");
            return;
        }

        if(form.email.trim().length < 5) {
            toast.error("Email musí mít alespoň 5 znaků.");
            return;
        }

        setSaving(true);

        const body = {
            firstName,
            lastName,
            email: form.email,
            class: form.class,
            gender: form.gender || null,
            schoolId: form.schoolId ? Number(form.schoolId) : null,
            accountType: form.accountType,
            avatarUrl: form.avatarUrl,
            bannerUrl: form.bannerUrl,
            enableReservations: form.enableReservations,
            sendLoginCredentialsEmail: form.sendLoginCredentialsEmail,
            password: form.password,
        };

        try {
            const response = await fetch(modalMode === "create" ? "/api/v1/account" : `/api/v1/account/${selectedAccount?.id}`, {
                method: modalMode === "create" ? "POST" : "PUT",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify(body),
            });

            if(!response.ok) throw new Error("Save failed");

            const data = await response.json() as {account: unknown, loginCredentialsEmailSent?: boolean};
            const savedAccount = AccountSchema.parse(data.account);
            const refreshed = await refreshAccounts();
            const nextSelected = refreshed.find(account => account.id === savedAccount.id) ?? savedAccount;

            if(loggedAccount?.id === nextSelected.id) {
                setLoggedAccount(nextSelected);
            }

            setSelectedAccount(nextSelected);
            setModalMode("detail");
            toast.success(modalMode === "create" ? `Uživatel ${nextSelected.fullName} vytvořen.` : `Uživatel ${nextSelected.fullName} upraven.`);
            if(form.sendLoginCredentialsEmail) {
                toast[data.loginCredentialsEmailSent ? "success" : "error"](
                    data.loginCredentialsEmailSent ? "Přihlašovací údaje byly odeslány na email." : "Uživatel je uložený, ale email se nepodařilo odeslat."
                );
            }
        } catch {
            toast.error("Změny se nepodařilo uložit.");
        } finally {
            setSaving(false);
        }
    };

    const deleteAccount = async () => {
        if(!selectedAccount || saving || !canManageSelectedAccount) return;

        setSaving(true);
        try {
            const response = await fetch(`/api/v1/account/${selectedAccount.id}`, {method: "DELETE"});
            if(!response.ok) throw new Error("Delete failed");

            await refreshAccounts();
            closeModal();
            toast.success("Uživatel smazán.");
        } catch {
            toast.error("Uživatele se nepodařilo smazat.");
        } finally {
            setSaving(false);
        }
    };

    const resetPassword = async () => {
        if(!selectedAccount || saving || !canManageSelectedAccount) return;

        setSaving(true);
        try {
            const response = await fetch(`/api/v1/account/${selectedAccount.id}/reset-password`, {method: "POST"});
            if(!response.ok) throw new Error("Reset failed");

            const data = await response.json() as {loginCredentialsEmailSent?: boolean};
            setModalMode("detail");
            toast.success(data.loginCredentialsEmailSent ? "Heslo resetováno a odesláno na email." : "Heslo resetováno, email se nepodařilo odeslat.");
        } catch {
            toast.error("Heslo se nepodařilo resetovat.");
        } finally {
            setSaving(false);
        }
    };

    return {
        deleteAccount,
        resetPassword,
        submitAccount,
    };
}
