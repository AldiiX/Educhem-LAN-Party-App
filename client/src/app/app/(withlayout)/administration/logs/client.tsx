"use client";

import style from "../layoutclient.module.scss";
import {useEffect} from "react";
import {useRouter} from "next/navigation";
import {useLogsAdministration} from "../_hooks/useAdministrationLogs";
import {LogsToolbar} from "./_components/LogsToolbar";
import {LogsFilters} from "./_components/LogsFilters";
import {LogsTable} from "./_components/LogsTable";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {hasRoleAtLeast} from "@/lib/roles";
import {Pagination} from "../_components/Pagination";
import {useRememberedCollapseState} from "@/hooks/useRememberedCollapseState";

type LogsProps = {
    initialFiltersCollapsed?: boolean;
};

export function Logs({initialFiltersCollapsed = false}: LogsProps) {
    const {account} = useAuth();
    const router = useRouter();
    const canManageApp = hasRoleAtLeast(account, "Admin");

    useEffect(() => {
        if (!canManageApp) {
            router.replace("/app/administration/users");
        }
    }, [canManageApp, router]);

    const logsAdministration = useLogsAdministration();
    const [filtersCollapsed, toggleFiltersCollapsed] = useRememberedCollapseState(
        initialFiltersCollapsed,
        "administrationLogsFiltersCollapsed",
    );
    const activeFilterCount = logsAdministration.selectedLogTypes.size
        + logsAdministration.selectedExactTypes.size
        + Number(Boolean(logsAdministration.dateFrom))
        + Number(Boolean(logsAdministration.dateTo))
        + Number(Boolean(logsAdministration.actorIdFilter))
        + Number(Boolean(logsAdministration.targetIdFilter));

    if (!canManageApp) {
        return null;
    }

    if(logsAdministration.logsError) {
        return <>
            <p>Logy se nepodařilo načíst.</p>

            <button
                type="button"
                onClick={() => logsAdministration.refreshLogs()}
            >
                Zkusit znovu
            </button>
        </>;
    }

    return <>
        <LogsToolbar
            totalCount={logsAdministration.totalItems}
            filteredCount={logsAdministration.pagination.totalEntries}
            searchTerm={logsAdministration.searchTerm}
            onSearchChange={logsAdministration.setSearchTerm}
            onClearFilters={logsAdministration.clearFilters}
            clearDisabled={
                logsAdministration.selectedLogTypes.size === 0
                && logsAdministration.selectedExactTypes.size === 0
                && !logsAdministration.dateFrom
                && !logsAdministration.dateTo
                && !logsAdministration.searchTerm
                && !logsAdministration.actorIdFilter
                && !logsAdministration.targetIdFilter
            }
        />

        {hasRoleAtLeast(logsAdministration.account, "Admin") && (
            <LogsFilters
                activeFilterCount={activeFilterCount}
                collapsed={filtersCollapsed}
                logTypes={logsAdministration.uniqueLogTypes}
                exactTypes={logsAdministration.uniqueExactTypes}
                selectedLogTypes={logsAdministration.selectedLogTypes}
                selectedExactTypes={logsAdministration.selectedExactTypes}
                logTypeCounts={logsAdministration.logTypeCounts}
                exactTypeCounts={logsAdministration.exactTypeCounts}
                dateFrom={logsAdministration.dateFrom}
                dateTo={logsAdministration.dateTo}
                actorIdFilter={logsAdministration.actorIdFilter}
                targetIdFilter={logsAdministration.targetIdFilter}
                onToggleLogType={logsAdministration.toggleLogType}
                onToggleExactType={logsAdministration.toggleExactType}
                onDateFromChange={logsAdministration.setDateFrom}
                onDateToChange={logsAdministration.setDateTo}
                onActorIdChange={logsAdministration.setActorIdFilter}
                onCollapseToggle={toggleFiltersCollapsed}
                onTargetIdChange={logsAdministration.setTargetIdFilter}
            />
        )}

        <section className={style.logsContent}>
            <LogsTable logs={logsAdministration.filteredLogs} loading={logsAdministration.logsValidating} />
        </section>

        <Pagination
            page={logsAdministration.pagination.page}
            pageSize={logsAdministration.pagination.pageSize}
            totalEntries={logsAdministration.pagination.totalEntries}
            totalPages={logsAdministration.pagination.totalPages}
            loading={logsAdministration.logsValidating}
            onPageChange={logsAdministration.setPage}
        />
    </>;
}

export const LogsTab = Logs;
