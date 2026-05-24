import {Dispatch, SetStateAction, useCallback, useMemo} from "react";
import {LogEntry} from "@/schemas/LogEntrySchema";

type UseLogsFiltersProps = {
    logs: LogEntry[];
    searchTerm: string;
    setSearchTerm: Dispatch<SetStateAction<string>>;
    selectedLogTypes: Set<string>;
    setSelectedLogTypes: Dispatch<SetStateAction<Set<string>>>;
    selectedExactTypes: Set<string>;
    setSelectedExactTypes: Dispatch<SetStateAction<Set<string>>>;
    dateFrom: string;
    setDateFrom: Dispatch<SetStateAction<string>>;
    dateTo: string;
    setDateTo: Dispatch<SetStateAction<string>>;
};

export function useLogsFilters({
   logs,
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
}: UseLogsFiltersProps){

    const uniqueExactTypes = useMemo(() => (
        Array.from(new Set(logs.map(log => log.exactType))).sort()
    ), [logs]);

    const uniqueLogTypes = useMemo(() => (
        Array.from(new Set(logs.map(log => log.type))).sort()
    ), [logs]);

    const logTypeCounts = useMemo(() => {
        const counts = new Map<string, number>();
        logs.forEach(log => {
            counts.set(log.type, (counts.get(log.type) ?? 0) + 1);
        });
        return counts;
    }, [logs]);

    const exactTypeCounts = useMemo(() => {
        const counts = new Map<string, number>();
        logs.forEach(log => {
            counts.set(log.exactType, (counts.get(log.exactType) ?? 0) + 1);
        });
        return counts;
    }, [logs]);

    const toggleLogType = useCallback((type: string) => {
        setSelectedLogTypes(prev => {
            const next = new Set(prev);
            if(next.has(type)) next.delete(type);
            else next.add(type);
            return next;
        });
    }, [setSelectedLogTypes]);

    const toggleExactType = useCallback((type: string) => {
        setSelectedExactTypes(prev => {
            const next = new Set(prev);
            if(next.has(type)) next.delete(type);
            else next.add(type);
            return next;
        });
    }, [setSelectedExactTypes]);

    const clearFilters = useCallback(() => {
        setSelectedLogTypes(new Set());
        setSelectedExactTypes(new Set());
        setDateFrom("");
        setDateTo("");
        setSearchTerm("");
    }, [setDateFrom, setDateTo, setSearchTerm, setSelectedExactTypes, setSelectedLogTypes]);

    const filteredLogs = useMemo(() => logs.filter((log) => {
        if(selectedLogTypes.size > 0 && !selectedLogTypes.has(String(log.type))) {
            return false;
        }

        if(selectedExactTypes.size > 0 && !selectedExactTypes.has(log.exactType)) {
            return false;
        }

        if(log.date) {
            if(dateFrom && log.date < new Date(dateFrom)) {
                return false;
            }
            if(dateTo && log.date > new Date(dateTo)) {
                return false;
            }
        }

        if(searchTerm && !log.message.toLowerCase().includes(searchTerm.toLowerCase())) {
            return false;
        }

        return true;
    }),[
        logs,
        selectedLogTypes,
        selectedExactTypes,
        dateFrom,
        dateTo,
        searchTerm,
    ]);

    return {
        uniqueExactTypes,
        uniqueLogTypes,
        logTypeCounts,
        exactTypeCounts,
        toggleLogType,
        toggleExactType,
        clearFilters,
        filteredLogs,
    };
}
