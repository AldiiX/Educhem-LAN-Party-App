import {FormEvent, useEffect, useMemo, useState} from "react";
import useSWR from "swr";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {fetcher} from "@/lib/swr";
import {hasRoleAtLeast, isSuperAdmin} from "@/lib/roles";
import {apiFetch} from "@/lib/apiClient";
import {
    AttendanceEntryType,
    AttendanceDeltaSchema,
    AttendanceEntry,
    AttendanceOverview,
    AttendanceOverviewSchema,
    AttendanceParticipant,
} from "@/schemas/AttendanceSchema";

export const attendanceActionLabels: Record<AttendanceEntryType, string> = {
    CheckIn: "Příchod",
    CheckOut: "Odchod",
};

const attendanceSearchDebounceMs = 350;

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

function matchesSearch(entry: AttendanceEntry, search: string): boolean {
    const query = search.trim().toLocaleLowerCase("cs-CZ");
    if(query.length === 0) return true;

    return [
        entry.profile.fullName,
        entry.profile.enrollment?.class ?? "",
        entry.reason ?? "",
        attendanceActionLabels[entry.type],
        entry.createdBy.fullName,
    ].some(value => value.toLocaleLowerCase("cs-CZ").includes(query));
}

export function useAttendance() {
    const {account} = useAuth();
    const [page, setPage] = useState(1);
    const [search, setSearchValue] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const attendanceUrl = useMemo(() => {
        const params = new URLSearchParams({page: String(page)});
        const query = debouncedSearch.trim();
        if(query.length > 0) params.set("search", query);
        return `/api/v1/attendance?${params.toString()}`;
    }, [debouncedSearch, page]);
    const {data, error, isLoading, isValidating, mutate} = useSWR(attendanceUrl, attendanceFetcher, {
        keepPreviousData: true,
    });
    const [selectedAccountId, setSelectedAccountId] = useState<string>("");
    const [reason, setReason] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);

    useEffect(() => {
        const timeout = window.setTimeout(() => setDebouncedSearch(search), attendanceSearchDebounceMs);
        return () => window.clearTimeout(timeout);
    }, [search]);

    const canManageAttendance = hasRoleAtLeast(account, "TeacherOrg");
    const canBypassAvailabilityLock = isSuperAdmin(account);
    const attendanceEnabled = data?.attendanceEnabled !== false || canBypassAvailabilityLock;
    const selectedParticipant = useMemo(() => {
        if(!data || !account) return null;
        const id = canManageAttendance && selectedAccountId ? selectedAccountId : account.id;
        return data.participants.find(participant => participant.profile.id === id)
            ?? (id === account.id ? buildSelfParticipant(account) : null);
    }, [account, canManageAttendance, data, selectedAccountId]);
    const nextType: AttendanceEntryType = selectedParticipant?.currentState === "CheckIn" ? "CheckOut" : "CheckIn";

    const updateSearch = (value: string) => {
        setSearchValue(value);
        setPage(1);
    };

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if(isSubmitting || !attendanceEnabled) return;

        setIsSubmitting(true);
        setSubmitError(null);

        try {
            const response = await apiFetch("/api/v1/attendance", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({
                    type: nextType,
                    accountId: canManageAttendance && selectedAccountId ? selectedAccountId : null,
                    reason,
                }),
            });

            if(response.status === 423) {
                const text = await response.text();
                throw new Error(text || "Docházka je momentálně uzamčená.");
            }

            if(!response.ok) {
                const text = await response.text();
                throw new Error(text || "Záznam se nepodařilo uložit.");
            }

            const delta = AttendanceDeltaSchema.parse(await response.json());
            setReason("");
            await mutate(currentData => {
                if(!currentData) return currentData;

                const addEntryToPage = page === 1 && matchesSearch(delta.entry, debouncedSearch);
                const totalEntries = addEntryToPage
                    ? currentData.pagination.totalEntries + 1
                    : currentData.pagination.totalEntries;

                const participantExists = currentData.participants.some(participant =>
                    participant.profile.id === delta.participant.profile.id
                );
                const participants = participantExists
                    ? currentData.participants.map(participant =>
                        participant.profile.id === delta.participant.profile.id ? delta.participant : participant
                    )
                    : [...currentData.participants, delta.participant]
                        .sort((a, b) => a.profile.fullName.localeCompare(b.profile.fullName, "cs-CZ"));

                return {
                    ...currentData,
                    entries: addEntryToPage
                        ? [delta.entry, ...currentData.entries.filter(entry => entry.id !== delta.entry.id)]
                            .slice(0, currentData.pagination.pageSize)
                        : currentData.entries,
                    participants,
                    stats: delta.stats,
                    pagination: {
                        ...currentData.pagination,
                        totalEntries,
                        totalPages: totalEntries === 0
                            ? 0
                            : Math.ceil(totalEntries / currentData.pagination.pageSize),
                    },
                };
            }, {revalidate: false});
            if(page !== 1) setPage(1);
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
        isValidating,
        canManageAttendance,
        attendanceEnabled,
        attendanceLocked: data?.attendanceEnabled === false,
        canBypassAvailabilityLock,
        selectedAccountId,
        selectedParticipant,
        reason,
        isSubmitting,
        submitError,
        search,
        page,
        nextType,
        mutate,
        setSelectedAccountId,
        setReason,
        setSearch: updateSearch,
        setPage,
        submit,
    };
}

export type AttendanceHook = ReturnType<typeof useAttendance>;
