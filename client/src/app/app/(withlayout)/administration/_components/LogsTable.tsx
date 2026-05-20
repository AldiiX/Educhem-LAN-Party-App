import style from "./LogsTable.module.scss";

type LogEntry = {
    id: number | string;
    type: string;
    exactType: string;
    message: string;
    date: string;
};

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
                <th>Zpráva</th>
                <th>Datum</th>
            </tr>
            </thead>

            <tbody>
            {logs.map((log) => (
                <tr key={log.id}>
                    <td className={style.logType}>{log.type}</td>
                    <td>{log.exactType}</td>
                    <td>{log.message}</td>
                    <td>{new Date(log.date).toLocaleString("cs-CZ")}</td>
                </tr>
            ))}
            </tbody>
        </table>
    </section>
}

