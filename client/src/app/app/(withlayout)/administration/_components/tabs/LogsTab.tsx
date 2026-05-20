"use client";

import {useCallback, useEffect, useMemo, useState} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {isSuperAdmin} from "@/lib/roles";
import style from "../../client.module.scss";

type LogEntry = {
    id: number | string;
    type: string;
    exactType: string;
    message: string;
    date: string;
};

export function LogsTab() {
    const {account} = useAuth();
    const [logs, setLogs] = useState<LogEntry[] | null>(null);
    const [logsError, setLogsError] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const [selectedLogTypes, setSelectedLogTypes] = useState<Set<string>>(() => new Set());
    const [selectedExactTypes, setSelectedExactTypes] = useState<Set<string>>(() => new Set());
    const [dateFrom, setDateFrom] = useState("");
    const [dateTo, setDateTo] = useState("");

    const fetchLogs = useCallback(async () => {
        setLogsError(false);
        try {
            const response = await fetch("/api/v1/adm/logs", {cache: "no-cache"});
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
        fetchLogs();
    }, [fetchLogs]);

    const uniqueExactTypes = useMemo(() => (
        Array.from(new Set((logs ?? []).map(log => log.exactType))).sort()
    ), [logs]);

    const uniqueLogTypes = useMemo(() => (
        Array.from(new Set((logs ?? []).map(log => log.type))).sort()
    ), [logs]);

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
    }), [logs, selectedLogTypes, selectedExactTypes, dateFrom, dateTo, searchTerm]);

    if(logsError) {
        return <>
            <p>Logy se nepodařilo načíst.</p>
            <button type="button" onClick={fetchLogs}>Zkusit znovu</button>
        </>;
    }

    if(logs === null) {
        return <p>Načítání logů...</p>;
    }

    return <section className={style.logsWrapper}>
        <section className={style.toolbar}>
            <div>
                <h2>Logy ({filteredLogs.length})</h2>
                <p>{logs.length} celkem</p>
            </div>
            <div className={style.searchBox}>
                <span style={{maskImage: "url(/icons/search.svg)"}}></span>
                <input
                    type="text"
                    placeholder="Hledat v logách..."
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                />
            </div>
            <button
                type="button"
                className={style.clearFiltersButton}
                onClick={clearFilters}
                disabled={
                    selectedLogTypes.size === 0
                    && selectedExactTypes.size === 0
                    && !dateFrom
                    && !dateTo
                    && !searchTerm
                }
            >
                Vyčistit filtry
            </button>
        </section>

        {isSuperAdmin(account) && (
            <section className={style.logsFilters}>
                <div className={style.logsFilterGroup}>
                    <p className={style.logsFilterLabel}>Typ logu:</p>
                    <div className={style.logsCheckboxes}>
                        {uniqueLogTypes.map((type) => (
                            <label key={type} className={style.logsCheckbox}>
                                <input
                                    type="checkbox"
                                    checked={selectedLogTypes.has(type)}
                                    onChange={() => toggleLogType(type)}
                                />
                                <span>{type}</span>
                            </label>
                        ))}
                    </div>
                </div>

                <div className={style.logsFilterGroup}>
                    <p className={style.logsFilterLabel}>Přesný typ:</p>
                    <div className={style.logsCheckboxes}>
                        {uniqueExactTypes.map((type) => (
                            <label key={type} className={style.logsCheckbox}>
                                <input
                                    type="checkbox"
                                    checked={selectedExactTypes.has(type)}
                                    onChange={() => toggleExactType(type)}
                                />
                                <span>{type}</span>
                            </label>
                        ))}
                    </div>
                </div>

                <div className={style.logsFilterGroup}>
                    <p className={style.logsFilterLabel}>Datum:</p>
                    <div className={style.logsDateFilters}>
                        <label className={style.logsDateField}>
                            <span>Od:</span>
                            <input
                                type="datetime-local"
                                value={dateFrom}
                                onChange={(event) => setDateFrom(event.target.value)}
                            />
                        </label>
                        <label className={style.logsDateField}>
                            <span>Do:</span>
                            <input
                                type="datetime-local"
                                value={dateTo}
                                onChange={(event) => setDateTo(event.target.value)}
                            />
                        </label>
                    </div>
                </div>
            </section>
        )}

        <table className={style.logsTable}>
            <thead>
                <tr>
                    <th>Typ</th>
                    <th>Přesný typ</th>
                    <th>Zpráva</th>
                    <th>Datum</th>
                </tr>
            </thead>
            <tbody>
                {filteredLogs.map((log) => (
                    <tr key={log.id}>
                        <td className={style.logType} data-type={log.type.toLowerCase()}>{log.type}</td>
                        <td>{log.exactType}</td>
                        <td>{log.message}</td>
                        <td>{new Date(log.date).toLocaleString("cs-CZ")}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    </section>;
}
