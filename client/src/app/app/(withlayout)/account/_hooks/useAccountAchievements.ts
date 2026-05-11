"use client";

import { useCallback, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Account, AccountSchema } from "@/schemas/AccountSchema";

export function useAccountAchievements(account: Account, setAccount: (account: Account) => void) {
    const [updatingAchievements, setUpdatingAchievements] = useState<Set<string>>(() => new Set());
    const [updatingBadges, setUpdatingBadges] = useState<Set<string>>(() => new Set());

    const achievementUpdatingIds = useMemo(() => updatingAchievements, [updatingAchievements]);
    const badgeUpdatingIds = useMemo(() => updatingBadges, [updatingBadges]);

    const toggleAchievementVisibility = useCallback(async (entryId: string, nextHidden: boolean) => {
        setUpdatingAchievements(prev => new Set(prev).add(entryId));
        try {
            const response = await fetch(`/api/v1/account/me/achievements/${entryId}`, {
                method: "PUT",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({isHidden: nextHidden}),
            });

            if(!response.ok) {
                toast.error("Nepodařilo se upravit achievement.");
                return;
            }

            const json = await response.json();
            const parsed = AccountSchema.safeParse(json);
            if(!parsed.success) {
                toast.error("Server vrátil neplatný profil.");
                return;
            }

            setAccount(parsed.data);
        } finally {
            setUpdatingAchievements(prev => {
                const next = new Set(prev);
                next.delete(entryId);
                return next;
            });
        }
    }, [setAccount]);

    const toggleBadgeTakenOut = useCallback(async (entryId: string, nextTakenOut: boolean) => {
        setUpdatingBadges(prev => new Set(prev).add(entryId));
        try {
            const response = await fetch(`/api/v1/account/me/badges/${entryId}`, {
                method: "PUT",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({isTakenOut: nextTakenOut}),
            });

            if(!response.ok) {
                toast.error("Nepodařilo se upravit badge.");
                return;
            }

            const json = await response.json();
            const parsed = AccountSchema.safeParse(json);
            if(!parsed.success) {
                toast.error("Server vrátil neplatný profil.");
                return;
            }

            setAccount(parsed.data);
        } finally {
            setUpdatingBadges(prev => {
                const next = new Set(prev);
                next.delete(entryId);
                return next;
            });
        }
    }, [setAccount]);

    return {
        achievementUpdatingIds,
        badgeUpdatingIds,
        toggleAchievementVisibility,
        toggleBadgeTakenOut,
    };
}

