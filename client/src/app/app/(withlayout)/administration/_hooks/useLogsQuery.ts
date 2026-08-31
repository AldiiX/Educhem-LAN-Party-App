import {useMemo} from "react";
import useSWR from "swr";
import {fetcher} from "@/lib/swr";
import {
    AdministrationLogsPage,
    AdministrationLogsPageSchema,
} from "@/schemas/AdministrationSchema";

const logsFetcher = async (url: string) => {
    return AdministrationLogsPageSchema.parse(await fetcher<unknown>(url));
};

const emptyPagination = {
    page: 1,
    pageSize: 25,
    totalEntries: 0,
    totalPages: 0,
};

export function useLogsQuery(queryString: string) {
    const url = `/api/v1/adm/logs${queryString ? `?${queryString}` : ""}`;
    const {data, error, isLoading, isValidating, mutate} = useSWR<AdministrationLogsPage>(url, logsFetcher, {
        keepPreviousData: true,
    });
    const logTypeCounts = useMemo(() => new Map(
        (data?.filterOptions.logTypes ?? []).map(option => [option.value, option.count])
    ), [data?.filterOptions.logTypes]);
    const exactTypeCounts = useMemo(() => new Map(
        (data?.filterOptions.exactTypes ?? []).map(option => [option.value, option.count])
    ), [data?.filterOptions.exactTypes]);

    const refreshLogs = async () => (await mutate())?.logs ?? [];

    return {
        exactTypeCounts,
        logs: data?.logs ?? [],
        logsError: error,
        logsLoading: isLoading,
        logsValidating: isValidating,
        logTypeCounts,
        mutateLogs: mutate,
        pagination: data?.pagination ?? emptyPagination,
        refreshLogs,
        totalItems: data?.totalItems ?? 0,
        uniqueExactTypes: (data?.filterOptions.exactTypes ?? []).map(option => option.value),
        uniqueLogTypes: (data?.filterOptions.logTypes ?? []).map(option => option.value),
    };
}
