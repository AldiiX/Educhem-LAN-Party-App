"use client";

import {useState} from "react";
import useSWR from "swr";
import {apiFetch} from "@/lib/apiClient";
import toast from "react-hot-toast";
import {AuthSession, AuthSessionSchema} from "@/schemas/AuthSessionSchema";

const sessionsFetcher = async (url: string) => {
    const res = await apiFetch(url);
    if (!res.ok) throw new Error("Nepodařilo se načíst relace.");
    const data = await res.json();
    return AuthSessionSchema.array().parse(data);
};

export function useAccountSessions() {
    const {data, error, isLoading, mutate} = useSWR<AuthSession[]>(
        "/api/v1/account/sessions",
        sessionsFetcher
    );

    const [revokingId, setRevokingId] = useState<string | null>(null);
    const [revokingOthers, setRevokingOthers] = useState(false);

    const sessions = data ?? [];

    const refreshSessions = async () => await mutate();

    const revokeSession = async (id: string) => {
        setRevokingId(id);
        try {
            const res = await apiFetch(`/api/v1/account/sessions/${id}`, {
                method: "DELETE",
            });
            if (!res.ok) {
                toast.error("Odhlášení zařízení se nezdařilo.");
                return false;
            }
            toast.success("Zařízení bylo úspěšně odhlášeno.");
            await mutate(sessions.filter(s => s.id !== id), {revalidate: true});
            return true;
        } catch {
            toast.error("Došlo k chybě při odhlašování zařízení.");
            return false;
        } finally {
            setRevokingId(null);
        }
    };

    const revokeOtherSessions = async () => {
        setRevokingOthers(true);
        try {
            const res = await apiFetch("/api/v1/account/sessions/other", {
                method: "DELETE",
            });
            if (!res.ok) {
                toast.error("Hromadné odhlášení zařízení se nezdařilo.");
                return false;
            }
            toast.success("Všechna ostatní zařízení byla odhlášena.");
            await mutate(sessions.filter(s => s.isCurrent), {revalidate: true});
            return true;
        } catch {
            toast.error("Došlo k chybě při odhlašování zařízení.");
            return false;
        } finally {
            setRevokingOthers(false);
        }
    };

    return {
        sessions,
        isLoading,
        error,
        revokingId,
        revokingOthers,
        refreshSessions,
        revokeSession,
        revokeOtherSessions,
    };
}
