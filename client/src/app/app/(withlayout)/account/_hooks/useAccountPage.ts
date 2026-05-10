"use client";

import {useState} from "react";
import {Account} from "@/schemas/AccountSchema";
import {AccountTab} from "./types";
import {useAccountModal} from "./useAccountModal";
import {useAccountPassword} from "./useAccountPassword";
import {useAccountProfile} from "./useAccountProfile";
import {useAccountSession} from "./useAccountSession";

export function useAccountPage(initialAccount: Account) {
    const [selectedTab, setSelectedTab] = useState<AccountTab>("overview");
    const session = useAccountSession(initialAccount);
    const modal = useAccountModal();
    const profile = useAccountProfile(session.account, session.setAccount, modal.closeModal);
    const password = useAccountPassword(session.setAccount, session.router);

    return {
        account: session.account,
        selectedTab,
        setSelectedTab,
        modal: modal.modal,
        setModal: modal.setModal,
        logout: session.logout,
        toggleTheme: session.toggleTheme,
        ...profile,
        ...password,
    };
}
