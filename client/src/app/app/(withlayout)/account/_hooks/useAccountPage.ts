"use client";

import {Account} from "@/schemas/AccountSchema";
import {useAccountModal} from "./useAccountModal";
import {useAccountPassword} from "./useAccountPassword";
import {useAccountProfile} from "./useAccountProfile";
import {useAccountSession} from "./useAccountSession";
import {useAccountAchievements} from "./useAccountAchievements";
import {useAccountPlatforms} from "./useAccountPlatforms";

export function useAccountPage(initialAccount: Account) {
    const session = useAccountSession(initialAccount);
    const modal = useAccountModal();
    const profile = useAccountProfile(session.account, session.setAccount, modal.closeModal);
    const password = useAccountPassword(session.setAccount, session.router);
    const achievements = useAccountAchievements(session.account, session.setAccount);
    const accountPlatforms = useAccountPlatforms(session.setAccount);

    return {
        account: session.account,
        modal: modal.modal,
        setModal: modal.setModal,
        platformErrorMessage: modal.platformErrorMessage,
        showPlatformError: modal.showPlatformError,
        logout: session.logout,
        toggleTheme: session.toggleTheme,
        ...achievements,
        ...accountPlatforms,
        ...profile,
        ...password,
    };
}
