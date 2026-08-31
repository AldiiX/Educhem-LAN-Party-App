import {useCallback, useMemo} from "react";
import {readPage, setRepeatedParam, useDebouncedQueryParam, useUrlQueryState} from "./useUrlQueryState";

function toIsoDate(value: string) {
    if(!value) return null;

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

export function useLogsFilters() {
    const {searchParams, updateQuery} = useUrlQueryState();
    const [searchTerm, setSearchTerm] = useDebouncedQueryParam("q", searchParams.get("q") ?? "", updateQuery);
    const [actorIdFilter, setActorIdFilter] = useDebouncedQueryParam("actorId", searchParams.get("actorId") ?? "", updateQuery);
    const [targetIdFilter, setTargetIdFilter] = useDebouncedQueryParam("targetId", searchParams.get("targetId") ?? "", updateQuery);
    const selectedLogTypes = useMemo(() => new Set(searchParams.getAll("logType")), [searchParams]);
    const selectedExactTypes = useMemo(() => new Set(searchParams.getAll("exactType")), [searchParams]);
    const dateFrom = searchParams.get("dateFrom") ?? "";
    const dateTo = searchParams.get("dateTo") ?? "";
    const page = readPage(searchParams.get("page"));
    const queryString = useMemo(() => {
        const params = new URLSearchParams(searchParams.toString());
        const from = toIsoDate(dateFrom);
        const to = toIsoDate(dateTo);

        if(from) params.set("dateFrom", from);
        else params.delete("dateFrom");
        if(to) params.set("dateTo", to);
        else params.delete("dateTo");

        return params.toString();
    }, [dateFrom, dateTo, searchParams]);

    const toggleLogType = useCallback((type: string) => {
        updateQuery(params => {
            const current = params.getAll("logType");
            setRepeatedParam(params, "logType", current.includes(type) ? current.filter(item => item !== type) : [...current, type]);
        });
    }, [updateQuery]);

    const toggleExactType = useCallback((type: string) => {
        updateQuery(params => {
            const current = params.getAll("exactType");
            setRepeatedParam(params, "exactType", current.includes(type) ? current.filter(item => item !== type) : [...current, type]);
        });
    }, [updateQuery]);

    const setDateFrom = useCallback((value: string) => {
        updateQuery(params => {
            if(value) params.set("dateFrom", value);
            else params.delete("dateFrom");
        });
    }, [updateQuery]);

    const setDateTo = useCallback((value: string) => {
        updateQuery(params => {
            if(value) params.set("dateTo", value);
            else params.delete("dateTo");
        });
    }, [updateQuery]);

    const clearFilters = useCallback(() => {
        setSearchTerm("");
        setActorIdFilter("");
        setTargetIdFilter("");
        updateQuery(params => {
            ["q", "actorId", "targetId", "logType", "exactType", "dateFrom", "dateTo"].forEach(key => params.delete(key));
        });
    }, [setActorIdFilter, setSearchTerm, setTargetIdFilter, updateQuery]);

    const setPage = useCallback((nextPage: number) => {
        updateQuery(params => {
            if(nextPage <= 1) params.delete("page");
            else params.set("page", String(nextPage));
        }, {resetPage: false});
    }, [updateQuery]);

    return {
        actorIdFilter,
        clearFilters,
        dateFrom,
        dateTo,
        page,
        queryString,
        searchTerm,
        selectedExactTypes,
        selectedLogTypes,
        setActorIdFilter,
        setDateFrom,
        setDateTo,
        setPage,
        setSearchTerm,
        setTargetIdFilter,
        targetIdFilter,
        toggleExactType,
        toggleLogType,
    };
}
