import {useState} from "react";
import useSWR from "swr";
import {fetcher} from "@/lib/swr";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {hasRoleAtLeast} from "@/lib/roles";
import {LogEntry, LogEntrySchema} from "@/schemas/LogEntrySchema";

const logsFetcher = async (url: string) => {
    const response = await fetcher<unknown>(url);

    return LogEntrySchema.array().parse(response ?? []);
};

export function useLogsQuery() {
    const {account} = useAuth();
    const canManageApp = hasRoleAtLeast(account, "Admin");

    const {data, error, isLoading, mutate} = useSWR<LogEntry[]>(
        canManageApp ? "/api/v1/adm/logs" : null,
        logsFetcher
    );
    const logs = data ?? [];

    const [searchTerm, setSearchTerm] = useState("");
    const [actorIdFilter, setActorIdFilter] = useState("");
    const [targetIdFilter, setTargetIdFilter] = useState("");

    const [selectedLogTypes, setSelectedLogTypes] = useState<Set<string>>(
        () => new Set()
    );

    const [selectedExactTypes, setSelectedExactTypes] = useState<Set<string>>(
        () => new Set()
    );

    const [dateFrom, setDateFrom] = useState("");
    const [dateTo, setDateTo] = useState("");

    const refreshLogs = async () => await mutate() ?? [];

    return {
        logs,
        logsError: error,
        logsLoading: isLoading,
        mutateLogs: mutate,

        refreshLogs,

        searchTerm,
        setSearchTerm,

        actorIdFilter,
        setActorIdFilter,

        targetIdFilter,
        setTargetIdFilter,

        selectedLogTypes,
        setSelectedLogTypes,

        selectedExactTypes,
        setSelectedExactTypes,

        dateFrom,
        setDateFrom,

        dateTo,
        setDateTo,
    };
}
