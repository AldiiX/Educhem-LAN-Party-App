"use client";

import {useState} from "react";
import toast from "react-hot-toast";
import {Account, AccountSchema} from "@/schemas/AccountSchema";
import {ProfileDraft} from "./types";

export function useAccountProfile(account: Account, setAccount: (account: Account) => void, closeModal: () => void) {
    const [savingProfile, setSavingProfile] = useState(false);
    const [profileDraft, setProfileDraft] = useState<ProfileDraft>(() => createProfileDraft(account));

    function resetProfileDraft() {
        setProfileDraft(createProfileDraft(account));
        closeModal();
    }

    async function saveProfile() {
        setSavingProfile(true);
        try {
            const response = await fetch("/api/v1/account/me", {
                method: "PUT",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({
                    gender: profileDraft.gender || null,
                    avatarUrl: profileDraft.avatarUrl,
                    bannerUrl: profileDraft.bannerUrl,
                }),
            });

            if(!response.ok) {
                toast.error("Profil se nepodařilo uložit.");
                return;
            }

            const json = await response.json();
            const parsed = AccountSchema.safeParse(json);
            if(!parsed.success) {
                toast.error("Server vrátil neplatný profil.");
                return;
            }

            setAccount(parsed.data);
            toast.success("Profil uložen.");
        } finally {
            setSavingProfile(false);
        }
    }

    return {
        profileDraft,
        setProfileDraft,
        savingProfile,
        resetProfileDraft,
        saveProfile,
    };
}

function createProfileDraft(account: Account): ProfileDraft {
    return {
        gender: account.gender ?? "",
        avatarUrl: account.avatarUrl ?? null,
        bannerUrl: account.bannerUrl ?? null,
    };
}
