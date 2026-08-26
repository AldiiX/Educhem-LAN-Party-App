"use client";

import {useCallback, useState} from "react";
import {AccountModal} from "./types";

export function useAccountModal() {
    const [modal, setModal] = useState<AccountModal>(null);
    const [platformErrorMessage, setPlatformErrorMessage] = useState<string | null>(null);

    const closeModal = useCallback(() => {
        setModal(null);
        setPlatformErrorMessage(null);
    }, []);

    const showPlatformError = useCallback((message: string) => {
        setPlatformErrorMessage(message);
        setModal("platform-error");
    }, []);

    return {
        modal,
        setModal,
        platformErrorMessage,
        showPlatformError,
        closeModal,
    };
}
