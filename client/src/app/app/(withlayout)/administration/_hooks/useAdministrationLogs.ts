import {useAuth} from "@/app/app/_providers/AuthProvider";

import {useLogsQuery} from "./useLogsQuery";
import {useLogsFilters} from "./useLogsFilters";

export function useLogsAdministration() {
    const {account} = useAuth();

    const logsQuery = useLogsQuery();

    const logsFilters = useLogsFilters({
        logs: logsQuery.logs,

        searchTerm: logsQuery.searchTerm,
        setSearchTerm: logsQuery.setSearchTerm,

        selectedLogTypes: logsQuery.selectedLogTypes,
        setSelectedLogTypes: logsQuery.setSelectedLogTypes,

        selectedExactTypes: logsQuery.selectedExactTypes,
        setSelectedExactTypes: logsQuery.setSelectedExactTypes,

        dateFrom: logsQuery.dateFrom,
        setDateFrom: logsQuery.setDateFrom,

        dateTo: logsQuery.dateTo,
        setDateTo: logsQuery.setDateTo,
    });

    return {
        account,

        ...logsQuery,
        ...logsFilters,
    };
}