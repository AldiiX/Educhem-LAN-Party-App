import {useCallback, useEffect, useState} from "react";

type LogEntry = {
    id: number | string;
    type: string;
    exactType: string;
    message: string;
    date: string;
};

export function useLogsQuery() {
    const [logs, setLogs] = useState<LogEntry[] | null>(null);

    const [logsError, setLogsError] = useState(false);

    const [searchTerm, setSearchTerm] = useState("");

    const [selectedLogTypes, setSelectedLogTypes] = useState<Set<string>>(
        () => new Set()
    );

    const [selectedExactTypes, setSelectedExactTypes] = useState<Set<string>>(
        () => new Set()
    );

    const [dateFrom, setDateFrom] = useState("");
    const [dateTo, setDateTo] = useState("");

    const refreshLogs = useCallback(async () => {
        setLogsError(false);

        try {
            const response = await fetch("/api/v1/adm/logs", {
                cache: "no-cache",
            });

            if(!response.ok) {
                setLogsError(true);
                return;
            }

            const json = await response.json();

            setLogs(Array.isArray(json) ? json : []);
        } catch {
            setLogsError(true);
        }
    }, []);

    useEffect(() => {
        refreshLogs();
    }, [refreshLogs]);

    return {
        logs,
        logsError,

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