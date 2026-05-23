import {FormEvent, useMemo, useState} from "react";
import useSWR from "swr";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {fetcher} from "@/lib/swr";
import {hasRoleAtLeast} from "@/lib/roles";
import {
    AttendanceEntryType,
    AttendanceOverview,
    AttendanceOverviewSchema,
    AttendanceParticipant,
} from "@/schemas/AttendanceSchema";

export const attendanceActionLabels: Record<AttendanceEntryType, string> = {
    CheckIn: "Příchod",
    CheckOut: "Odchod",
};

const attendanceFetcher = async (url: string): Promise<AttendanceOverview> => {
    return AttendanceOverviewSchema.parse(await fetcher<unknown>(url));
};

function buildSelfParticipant(account: AttendanceParticipant["profile"]): AttendanceParticipant {
    return {
        profile: account,
        currentState: null,
        latestEntry: null,
    };
}

export function useAttendance() {
    const {account} = useAuth();
    const {data, error, isLoading, mutate} = useSWR("/api/v1/attendance", attendanceFetcher);
    const [selectedAccountId, setSelectedAccountId] = useState<string>("");
    const [reason, setReason] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);
    const [search, setSearch] = useState("");

    const canManageAttendance = hasRoleAtLeast(account, "TeacherOrg");
    const selectedParticipant = useMemo(() => {
        if(!data || !account) return null;
        const id = canManageAttendance && selectedAccountId ? selectedAccountId : account.id;
        return data.participants.find(participant => participant.profile.id === id)
            ?? (id === account.id ? buildSelfParticipant(account) : null);
    }, [account, canManageAttendance, data, selectedAccountId]);
    const nextType: AttendanceEntryType = selectedParticipant?.currentState === "CheckIn" ? "CheckOut" : "CheckIn";
    const filteredEntries = useMemo(() => {
        const query = search.trim().toLowerCase();
        if(!data) return [];
        if(query.length === 0) return data.entries;

        return data.entries.filter(entry => [
            entry.profile.fullName,
            entry.profile.class ?? "",
            entry.reason ?? "",
            attendanceActionLabels[entry.type],
            entry.createdBy.fullName,
        ].some(value => value.toLowerCase().includes(query)));
    }, [data, search]);

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if(isSubmitting) return;

        setIsSubmitting(true);
        setSubmitError(null);

        try {
            const response = await fetch("/api/v1/attendance", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({
                    type: nextType,
                    accountId: canManageAttendance && selectedAccountId ? selectedAccountId : null,
                    reason,
                }),
            });

            if(!response.ok) {
                const text = await response.text();
                throw new Error(text || "Záznam se nepodařilo uložit.");
            }

            setReason("");
            await mutate();
        } catch (err) {
            setSubmitError(err instanceof Error ? err.message : "Záznam se nepodařilo uložit.");
        } finally {
            setIsSubmitting(false);
        }
    };

    return {
        account,
        data,
        error,
        isLoading,
        canManageAttendance,
        selectedAccountId,
        selectedParticipant,
        reason,
        isSubmitting,
        submitError,
        search,
        nextType,
        filteredEntries,
        mutate,
        setSelectedAccountId,
        setReason,
        setSearch,
        submit,
    };
}

export type AttendanceHook = ReturnType<typeof useAttendance>;
