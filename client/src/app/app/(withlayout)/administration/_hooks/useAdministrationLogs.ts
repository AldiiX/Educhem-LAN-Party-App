import {useEffect} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";

import {useLogsQuery} from "./useLogsQuery";
import {useLogsFilters} from "./useLogsFilters";

export function useLogsAdministration() {
    const {account} = useAuth();

    const logsFilters = useLogsFilters();
    const logsQuery = useLogsQuery(logsFilters.queryString);

    useEffect(() => {
        if(!logsQuery.logsValidating && logsQuery.pagination.page !== logsFilters.page) {
            logsFilters.setPage(logsQuery.pagination.page);
        }
    }, [logsFilters.page, logsFilters.setPage, logsQuery.logsValidating, logsQuery.pagination.page]);

    return {
        account,

        ...logsQuery,
        ...logsFilters,
        filteredLogs: logsQuery.logs,
    };
}
