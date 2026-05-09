import {useState} from "react";
import {Account} from "@/schemas/AccountSchema";
import {accountToForm} from "./accountForm";
import {emptyForm} from "./constants";
import {AccountForm, ModalMode} from "./types";

export function useAccountModalState(saving: boolean) {
    const [modalMode, setModalMode] = useState<ModalMode | null>(null);
    const [selectedAccount, setSelectedAccount] = useState<Account | null>(null);
    const [form, setForm] = useState<AccountForm>(emptyForm);

    const openModal = (mode: ModalMode, account: Account | null = null) => {
        setSelectedAccount(account);
        setForm(accountToForm(account));
        setModalMode(mode);
    };

    const closeModal = () => {
        if(saving) return;
        setModalMode(null);
        setSelectedAccount(null);
        setForm(emptyForm);
    };

    return {
        closeModal,
        form,
        modalMode,
        openModal,
        selectedAccount,
        setForm,
        setModalMode,
        setSelectedAccount,
    };
}
