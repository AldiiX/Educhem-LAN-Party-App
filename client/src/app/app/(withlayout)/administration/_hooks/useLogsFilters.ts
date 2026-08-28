import {Dispatch, SetStateAction, useCallback, useMemo} from "react";
import {LogEntry} from "@/schemas/LogEntrySchema";

type UseLogsFiltersProps = {
    logs: LogEntry[];
    searchTerm: string;
    setSearchTerm: Dispatch<SetStateAction<string>>;
    actorIdFilter: string;
    setActorIdFilter: Dispatch<SetStateAction<string>>;
    targetIdFilter: string;
    setTargetIdFilter: Dispatch<SetStateAction<string>>;
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
        setActorIdFilter("");
        setTargetIdFilter("");
    }, [setDateFrom, setDateTo, setSearchTerm, setActorIdFilter, setTargetIdFilter, setSelectedExactTypes, setSelectedLogTypes]);

    const filteredLogs = useMemo(() => logs.filter((log) => {
        if(selectedLogTypes.size > 0 && !selectedLogTypes.has(String(log.type))) {
            return false;
        }

        if(selectedExactTypes.size > 0 && !selectedExactTypes.has(log.exactType)) {
            return false;
        }

        if(actorIdFilter && (!log.actorId || !log.actorId.toLowerCase().includes(actorIdFilter.toLowerCase().trim()))) {
            return false;
        }

        if(targetIdFilter && (!log.targetId || !log.targetId.toLowerCase().includes(targetIdFilter.toLowerCase().trim()))) {
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

        if(searchTerm) {
            const term = searchTerm.toLowerCase().trim();
            const matchMessage = log.message.toLowerCase().includes(term);
            const matchActor = log.actorId?.toLowerCase().includes(term);
            const matchTarget = log.targetId?.toLowerCase().includes(term);
            const matchExact = log.exactType.toLowerCase().includes(term);
            if (!matchMessage && !matchActor && !matchTarget && !matchExact) {
                return false;
            }
        }

        return true;
    }),[
        logs,
        selectedLogTypes,
        selectedExactTypes,
        actorIdFilter,
        targetIdFilter,
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
