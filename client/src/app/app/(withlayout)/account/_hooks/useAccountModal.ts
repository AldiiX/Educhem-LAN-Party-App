"use client";

import {useState} from "react";
import {AccountModal} from "./types";

export function useAccountModal() {
    const [modal, setModal] = useState<AccountModal>(null);

    return {
        modal,
        setModal,
        closeModal: () => setModal(null),
    };
}
