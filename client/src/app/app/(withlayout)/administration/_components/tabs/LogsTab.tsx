"use client";

import style from "../../client.module.scss";

import {useLogsAdministration} from "../../_hooks/useAdministrationLogs";

import {LogsToolbar} from "../LogsToolbar";
import {LogsFilters} from "../LogsFilters";
import {LogsTable} from "../LogsTable";

import {isSuperAdmin} from "@/lib/roles";

export function LogsTab() {
    const logsAdministration = useLogsAdministration();

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
            totalCount={logsAdministration.logs.length}
            filteredCount={logsAdministration.filteredLogs.length}
            searchTerm={logsAdministration.searchTerm}
            onSearchChange={logsAdministration.setSearchTerm}
            onClearFilters={logsAdministration.clearFilters}
            clearDisabled={
                logsAdministration.selectedLogTypes.size === 0
                && logsAdministration.selectedExactTypes.size === 0
                && !logsAdministration.dateFrom
                && !logsAdministration.dateTo
                && !logsAdministration.searchTerm
            }
        />

        {isSuperAdmin(logsAdministration.account) && (
            <LogsFilters
                logTypes={logsAdministration.uniqueLogTypes}
                exactTypes={logsAdministration.uniqueExactTypes}
                selectedLogTypes={logsAdministration.selectedLogTypes}
                selectedExactTypes={logsAdministration.selectedExactTypes}
                logTypeCounts={logsAdministration.logTypeCounts}
                exactTypeCounts={logsAdministration.exactTypeCounts}
                dateFrom={logsAdministration.dateFrom}
                dateTo={logsAdministration.dateTo}
                onToggleLogType={logsAdministration.toggleLogType}
                onToggleExactType={logsAdministration.toggleExactType}
                onDateFromChange={logsAdministration.setDateFrom}
                onDateToChange={logsAdministration.setDateTo}
            />
        )}

        <section className={style.logsContent}>
            <LogsTable logs={logsAdministration.filteredLogs} />
        </section>
    </>;
}
