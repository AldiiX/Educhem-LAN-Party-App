import style from "./LogsTable.module.scss";
import {LogEntry} from "@/schemas/LogEntrySchema";

type LogsTableProps = {
    logs: LogEntry[];
};

export function LogsTable({logs}: LogsTableProps) {
    return <section className={style.logsTableWrapper}>
        <table className={style.logsTable}>
            <thead>
            <tr>
                <th>Typ</th>
                <th>Přesný typ</th>
                <th>Aktér ID</th>
                <th>Cíl ID</th>
                <th>Zpráva</th>
                <th>Datum</th>
            </tr>
            </thead>

            <tbody>
            {logs.map((log) => (
                <tr key={log.id}>
                    <td className={style.logType}>{log.type}</td>
                    <td className={style.exactType}>{log.exactType}</td>
                    <td className={style.idCell} title={log.actorId ?? undefined}>
                        {log.actorId ? (
                            <code className={style.idBadge}>{log.actorId.slice(0, 8)}…</code>
                        ) : "-"}
                    </td>
                    <td className={style.idCell} title={log.targetId ?? undefined}>
                        {log.targetId ? (
                            <code className={style.idBadge}>
                                {log.targetId.length > 14 ? `${log.targetId.slice(0, 14)}…` : log.targetId}
                            </code>
                        ) : "-"}
                    </td>
                    <td className={style.message}>{log.message}</td>
                    <td className={style.date}>{log.date ? log.date.toLocaleString("cs-CZ") : "-"}</td>
                </tr>
            ))}
            </tbody>
        </table>
    </section>;
}
