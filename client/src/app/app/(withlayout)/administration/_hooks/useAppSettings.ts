import {FormEvent, useEffect, useMemo, useState} from "react";
import useSWR from "swr";
import {toast} from "react-hot-toast";
import {
    AppSettingsResponse,
    mapAppSettings,
    ReservationStatusType
} from "@/schemas/AppSettingsSchema";

const fetcher = async (url: string) => {
    const res = await fetch(url);

    if (!res.ok) {
        throw new Error("Failed to load app settings");
    }

    return await res.json() as AppSettingsResponse;
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

        const now = Date.now();
        const from = appSettings.reservationsEnabledFrom.getTime();
        const to = appSettings.reservationsEnabledTo.getTime();

        let target: number | null = null;

        if (now < from) {
            target = from;
        } else if (now < to) {
            target = to;
        }

        if (target === null) {
            return;
        }

        const diff = target - now;
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

    async function submitChat(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const form = event.currentTarget;
        const formData = new FormData(form);

        setSaving(true);

        const res = await fetch("/api/v1/appsettings", {
            method: "PUT",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({
                chatEnabled: formData.get("chatEnabled") === "true",
            }),
        });

        setSaving(false);

        if (!res.ok) {
            toast.error("Nastavení chatu se nepodařilo uložit.");
            return;
        }

        toast.success("Nastavení chatu bylo uloženo.");
        await mutate();
    }

    return {
        appSettings,
        error,
        isLoading,
        saving,
        reservationsStatus,
        setReservationsStatus,
        submitReservations,
        submitChat,
        mutate,
    };
}