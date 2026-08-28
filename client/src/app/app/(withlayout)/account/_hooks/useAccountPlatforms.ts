"use client";

import {useState} from "react";
import toast from "react-hot-toast";
import {Account, AccountSchema, AvatarSyncPlatform} from "@/schemas/AccountSchema";
import {ConnectablePlatform, platforms} from "@/data/platforms";
import {apiFetch} from "@/lib/apiClient";

export function useAccountPlatforms(setAccount: (account: Account) => void) {
    const [platformLoading, setPlatformLoading] = useState(false);

    function connectPlatform(platform: ConnectablePlatform) {
        window.location.assign(`/api/v1/${platform}/connect`);
    }

    async function disconnectPlatform(platform: ConnectablePlatform) {
        const platformName = platforms.find(item => item.id === platform)?.name ?? platform;
        setPlatformLoading(true);
        try {
            const response = await apiFetch(`/api/v1/${platform}/connection`, {method: "DELETE"});
            if(!response.ok) {
                toast.error(`${platformName} se nepodařilo odpojit.`);
                return;
            }

            const account = AccountSchema.safeParse(await response.json());
            if(!account.success) {
                toast.error("Server vrátil neplatný účet.");
                return;
            }

            setAccount(account.data);
            toast.success(`${platformName} odpojen.`);
        } finally {
            setPlatformLoading(false);
        }
    }

	async function setAvatarSyncPlatform(platform: AvatarSyncPlatform | null) {
		setPlatformLoading(true);
		try {
			const response = await apiFetch("/api/v1/account/avatar-sync-platform", {
				method: "PUT",
				headers: {"Content-Type": "application/json"},
				body: JSON.stringify({platform}),
            });
            if(!response.ok) {
				toast.error("Zdroj synchronizace avataru se nepodařilo změnit.");
                return;
            }

            const account = AccountSchema.safeParse(await response.json());
            if(!account.success) {
                toast.error("Server vrátil neplatný účet.");
                return;
            }

            setAccount(account.data);
			toast.success(platform === "Discord" && !account.data.discordUsername
				? "Discord zvolen. Avatar se synchronizuje po propojení."
				: platform ? `Avatar se synchronizuje z ${platform}.` : "Synchronizace avataru vypnuta.");
        } finally {
            setPlatformLoading(false);
        }
    }

    return {
		platformLoading,
		connectPlatform,
		disconnectPlatform,
		setAvatarSyncPlatform,
    };
}
