"use client";

import {useState} from "react";
import toast from "react-hot-toast";
import {Account, AccountSchema, AvatarSyncPlatform} from "@/schemas/AccountSchema";

export function useAccountDiscord(setAccount: (account: Account) => void) {
    const [discordLoading, setDiscordLoading] = useState(false);

    function connectDiscord() {
        window.location.assign("/api/v1/discord/connect");
    }

    async function disconnectDiscord() {
        setDiscordLoading(true);
        try {
            const response = await fetch("/api/v1/discord/connection", {method: "DELETE"});
            if(!response.ok) {
                toast.error("Discord se nepodařilo odpojit.");
                return;
            }

            const account = AccountSchema.safeParse(await response.json());
            if(!account.success) {
                toast.error("Server vrátil neplatný účet.");
                return;
            }

            setAccount(account.data);
            toast.success("Discord odpojen.");
        } finally {
            setDiscordLoading(false);
        }
    }

	async function setAvatarSyncPlatform(platform: AvatarSyncPlatform | null) {
		setDiscordLoading(true);
		try {
			const response = await fetch("/api/v1/account/avatar-sync-platform", {
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
            setDiscordLoading(false);
        }
    }

    return {
        discordLoading,
        connectDiscord,
        disconnectDiscord,
		setAvatarSyncPlatform,
    };
}
