import {FormEvent, useEffect, useMemo, useState} from "react";
import useSWR from "swr";
import {toast} from "react-hot-toast";
import {
    AppSettingsResponse,
    mapAppSettings,
    ReservationStatusType
} from "@/schemas/AppSettingsSchema";

const fetcher = async (url: string) => {
    const res = await fetch(url, {credentials: "include"});

    if (!res.ok) {
        throw new Error("Failed to load app settings");
    }

    return await res.json() as AppSettingsResponse;
};

export type CacheClearResult = {
    removedKeys: number;
    trackedKeysBefore: number;
    trackedKeysAfter: number;
    managedMemoryBeforeBytes: number;
    managedMemoryAfterBytes: number;
};

export function useAppSettings() {
    const {data, error, isLoading, mutate} = useSWR("/api/v1/appsettings", fetcher, {
        revalidateOnFocus: false,
        revalidateOnReconnect: false,
        refreshInterval: 0
    });

    const appSettings = useMemo(() => data ? mapAppSettings(data) : null, [data]);

    const [reservationsStatus, setReservationsStatus] = useState<ReservationStatusType>("Closed");
    const [saving, setSaving] = useState(false);
    const [clearingCache, setClearingCache] = useState(false);
    const [lastCacheClear, setLastCacheClear] = useState<CacheClearResult | null>(null);

    useEffect(() => {
        if (!appSettings) {
            return;
        }

        setReservationsStatus(appSettings.reservationsStatus);
    }, [appSettings]);

    useEffect(() => {
        if (!appSettings || appSettings.reservationsStatus !== "UseTimer") {
            return;
        }

        const serverNow = appSettings.serverNow.getTime();
        const from = appSettings.reservationsEnabledFrom.getTime();
        const to = appSettings.reservationsEnabledTo.getTime();

        if (!Number.isFinite(serverNow) || !Number.isFinite(from) || !Number.isFinite(to)) {
            return;
        }

        let target: number | null = null;

        if (serverNow < from) {
            target = from;
        } else if (serverNow < to) {
            target = to;
        }

        if (target === null) {
            return;
        }

        const diff = target - serverNow;
        const maxTimeout = 2147483647;

        if (diff <= 0 || diff > maxTimeout) {
            return;
        }

        const timeout = window.setTimeout(() => {
            void mutate();
        }, diff + 500);

        return () => window.clearTimeout(timeout);
    }, [appSettings, mutate]);

    async function submitReservations(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const form = event.currentTarget;
        const formData = new FormData(form);

        const status = formData.get("reservationsStatus") as ReservationStatusType;
        const from = formData.get("reservationsEnabledFrom") as string | null;
        const to = formData.get("reservationsEnabledTo") as string | null;

        setSaving(true);

        const res = await fetch("/api/v1/appsettings", {
            method: "PUT",
            credentials: "include",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({
                reservationsStatus: status,
                reservationsEnabledFrom: from ? new Date(from).toISOString() : null,
                reservationsEnabledTo: to ? new Date(to).toISOString() : null,
            }),
        });

        setSaving(false);

        if (!res.ok) {
            toast.error("Nastavení rezervací se nepodařilo uložit.");
            return;
        }

        toast.success("Nastavení rezervací bylo uloženo.");
        await mutate();
    }

    async function submitFeatureSettings(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const form = event.currentTarget;
        const formData = new FormData(form);

        setSaving(true);

        const res = await fetch("/api/v1/appsettings", {
            method: "PUT",
            credentials: "include",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({
                chatEnabled: formData.get("chatEnabled") === "true",
                attendanceEnabled: formData.get("attendanceEnabled") === "true",
                problemReportsEnabled: formData.get("problemReportsEnabled") === "true",
            }),
        });

        setSaving(false);

        if (!res.ok) {
            toast.error("Nastavení funkcí se nepodařilo uložit.");
            return;
        }

        toast.success("Nastavení funkcí bylo uloženo.");
        await mutate();
    }

    async function clearAppCache() {
        setClearingCache(true);

        try {
            const res = await fetch("/api/v1/appsettings/cache/clear", {
                method: "POST",
                credentials: "include",
            });

            if (!res.ok) {
                toast.error("Cache aplikace se nepodařilo smazat.");
                return;
            }

            const result = await res.json() as CacheClearResult;
            setLastCacheClear(result);
            toast.success(`Cache aplikace byla smazána (${result.removedKeys} klíčů).`);
            await mutate();
        } catch {
            toast.error("Cache aplikace se nepodařilo smazat.");
        } finally {
            setClearingCache(false);
        }
    }

    return {
        appSettings,
        error,
        isLoading,
        saving,
        clearingCache,
        lastCacheClear,
        reservationsStatus,
        setReservationsStatus,
        submitReservations,
        submitFeatureSettings,
        clearAppCache,
        mutate,
    };
}
