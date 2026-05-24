import {useState} from "react";
import useSWR from "swr";
import {fetcher} from "@/lib/swr";
import {LogEntry, LogEntrySchema} from "@/schemas/LogEntrySchema";

const logsFetcher = async (url: string) => {
    const response = await fetcher<unknown>(url);

    return LogEntrySchema.array().parse(response ?? []);
};

export function useLogsQuery() {
    const {data, error, isLoading, mutate} = useSWR<LogEntry[]>("/api/v1/adm/logs", logsFetcher);
    const logs = data ?? [];

    const [searchTerm, setSearchTerm] = useState("");

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
