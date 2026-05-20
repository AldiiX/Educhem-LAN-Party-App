import { useCallback, useEffect, useMemo, useState } from "react";

type LogEntry = {
    id: number | string;
    type: string;
    exactType: string;
    message: string;
    date: string;
};

type UseLogsFiltersProps = {
    logs: LogEntry[] | null;
    searchTerm: string;
    setSearchTerm: React.Dispatch<React.SetStateAction<string>>;
    selectedLogTypes: Set<string>;
    setSelectedLogTypes: React.Dispatch<React.SetStateAction<Set<string>>>;
    selectedExactTypes: Set<string>;
    setSelectedExactTypes: React.Dispatch<React.SetStateAction<Set<string>>>;
    dateFrom: string;
    setDateFrom: React.Dispatch<React.SetStateAction<string>>;
    dateTo: string;
    setDateTo: React.Dispatch<React.SetStateAction<string>>;
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
        Array.from(new Set((logs ?? []).map(log => log.exactType))).sort()
    ), [logs]);

    const uniqueLogTypes = useMemo(() => (
        Array.from(new Set((logs ?? []).map(log => log.type))).sort()
    ), [logs]);

    const logTypeCounts = useMemo(() => {
        const counts = new Map<string, number>();
        (logs ?? []).forEach(log => {
            counts.set(log.type, (counts.get(log.type) ?? 0) + 1);
        });
        return counts;
    }, [logs]);

    const exactTypeCounts = useMemo(() => {
        const counts = new Map<string, number>();
        (logs ?? []).forEach(log => {
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
    }, []);

    const toggleExactType = useCallback((type: string) => {
        setSelectedExactTypes(prev => {
            const next = new Set(prev);
            if(next.has(type)) next.delete(type);
            else next.add(type);
            return next;
        });
    }, []);

    const clearFilters = useCallback(() => {
        setSelectedLogTypes(new Set());
        setSelectedExactTypes(new Set());
        setDateFrom("");
        setDateTo("");
        setSearchTerm("");
    }, []);

    const filteredLogs = useMemo(() => (logs ?? []).filter((log) => {
        if(selectedLogTypes.size > 0 && !selectedLogTypes.has(String(log.type))) {
            return false;
        }

        if(selectedExactTypes.size > 0 && !selectedExactTypes.has(log.exactType)) {
            return false;
        }

        const logDate = new Date(log.date);
        if(dateFrom && logDate < new Date(dateFrom)) {
            return false;
        }
        if(dateTo && logDate > new Date(dateTo)) {
            return false;
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
