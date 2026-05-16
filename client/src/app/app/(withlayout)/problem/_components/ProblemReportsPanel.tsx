import {useState} from "react";
import {Avatar} from "@/components/Avatar";
import style from "./ProblemReportsPanel.module.scss";
import type {ProblemReportHook, ProblemReportItem, ProblemReportPriority, ProblemReportStatus} from "../_hooks/useProblemReport";

type ProblemReportsPanelProps = {
    report: ProblemReportHook;
    canManageReports: boolean;
};

export function ProblemReportsPanel({report, canManageReports}: ProblemReportsPanelProps) {
    const [expandedId, setExpandedId] = useState<string | null>(null);

    const toggleReport = (id: string) => {
        setExpandedId(currentId => currentId === id ? null : id);
    };
    const hasActiveFilters = report.search.length > 0 || report.statusFilter !== "all" || report.priorityFilter !== "all";

    return <section className={style.panel}>
        <div className={style.header}>
            <div>
                <h2>{canManageReports ? "Všechna nahlášení" : "Vaše nahlášení"}</h2>
                <p>{report.filteredReports.length} z {report.reports.length} celkem</p>
            </div>
            {report.isLoadingReports && <span>Načítám...</span>}
        </div>

        <div className={style.filters}>
            <label className={style.searchBox}>
                <span style={{maskImage: "url(/icons/forum.svg)"}}></span>
                <input
                    value={report.search}
                    onChange={event => report.setSearch(event.target.value)}
                    placeholder="Hledat podle názvu, popisu, uživatele..."
                />
            </label>

            <label>
                <span>Status</span>
                <select value={report.statusFilter} onChange={event => report.setStatusFilter(event.target.value as ProblemReportStatus | "all")}>
                    <option value="all">Všechny</option>
                    {report.statusOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                </select>
            </label>

            <label>
                <span>Priorita</span>
                <select value={report.priorityFilter} onChange={event => report.setPriorityFilter(event.target.value as ProblemReportPriority | "all")}>
                    <option value="all">Všechny</option>
                    {report.priorityOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                </select>
            </label>

            {hasActiveFilters && <button type="button" onClick={report.clearFilters}>Vyčistit</button>}
        </div>

        {report.reportsError && <p className={style.error}>Hlášení se nepodařilo načíst.</p>}
        {report.isLoadingReports && <ProblemReportsSkeleton canManageReports={canManageReports} />}
        {!report.isLoadingReports && report.reports.length === 0 && <p className={style.empty}>Zatím tu nejsou žádná hlášení.</p>}
        {!report.isLoadingReports && report.reports.length > 0 && report.filteredReports.length === 0 && <p className={style.empty}>Filtru neodpovídá žádné hlášení.</p>}

        {!report.isLoadingReports && report.filteredReports.length > 0 && <div className={`${style.table} ${canManageReports ? style.withReporter : ""}`}>
            <div className={style.tableHead}>
                <span>Název</span>
                <span>Kategorie</span>
                <span>Priorita</span>
                <span>Status</span>
                <span>Vytvořeno</span>
                {canManageReports && <span>Uživatel</span>}
            </div>

            {report.filteredReports.map(item => (
                <ProblemReportRow
                    key={item.id}
                    item={item}
                    report={report}
                    canManageReports={canManageReports}
                    expanded={expandedId === item.id}
                    onToggle={() => toggleReport(item.id)}
                />
            ))}
        </div>}
    </section>;
}

function ProblemReportRow({item, report, canManageReports, expanded, onToggle}: {
    item: ProblemReportItem;
    report: ProblemReportHook;
    canManageReports: boolean;
    expanded: boolean;
    onToggle: () => void;
}) {
    const draft = report.getResolveDraft(item.id);

    return <article className={`${style.rowShell} ${expanded ? style.expanded : ""}`}>
        <button type="button" className={style.row} onClick={onToggle} aria-expanded={expanded}>
            <span>
                <strong>{item.title}</strong>
                <small>{item.description}</small>
            </span>
            <span>{report.categoryLabels[item.category]}</span>
            <span>{report.priorityLabels[item.priority]}</span>
            <span className={`${style.status} ${style[item.status]}`}>{report.statusLabels[item.status]}</span>
            <span>{formatDate(item.createdAtUtc)}</span>
            {canManageReports && <span className={style.personCompact}>
                <Avatar name={item.reporter.fullName} size="28px" src={item.reporter.avatarUrl} />
                <p>{item.reporter.fullName}</p>
            </span>}
        </button>

        <div className={style.detailWrap} aria-hidden={!expanded}>
            <div className={style.detail}>
                <p className={style.description}>{item.description}</p>

                <dl className={style.meta}>
                    <PersonMeta label="Uživatel" account={item.reporter} />
                    <Meta label="Kontakt" value={item.contact ?? "Neuvedeno"} />
                    <Meta label="Vytvořeno" value={formatDate(item.createdAtUtc)} />
                    <Meta label="Vyřešeno" value={item.resolvedAtUtc ? formatDate(item.resolvedAtUtc) : "Zatím ne"} />
                    {item.resolvedBy && <PersonMeta label="Vyřešil/a" account={item.resolvedBy} />}
                </dl>

                {item.resolutionNote && <div className={style.note}>
                    <strong>Poznámka</strong>
                    <p>{item.resolutionNote}</p>
                </div>}

                {canManageReports && <div className={style.actions}>
                    <select
                        value={draft.status}
                        onChange={event => report.updateResolveDraft(item.id, "status", event.target.value as ProblemReportStatus)}
                    >
                        {report.statusOptions.map(option => (
                            <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                    </select>
                    <textarea
                        value={draft.resolutionNote}
                        onChange={event => report.updateResolveDraft(item.id, "resolutionNote", event.target.value)}
                        placeholder="Poznámka k řešení"
                        rows={3}
                    />
                    <div>
                        <button type="button" onClick={() => report.resolveReport(item.id)} disabled={report.pendingReportId === item.id}>Uložit stav</button>
                        <button type="button" className={style.delete} onClick={() => report.deleteReport(item.id)} disabled={report.pendingReportId === item.id}>Smazat</button>
                    </div>
                </div>}
            </div>
        </div>
    </article>;
}

function ProblemReportsSkeleton({canManageReports}: {canManageReports: boolean}) {
    const columns = canManageReports ? 6 : 5;

    return <div className={`${style.table} ${style.skeletonTable} ${canManageReports ? style.withReporter : ""}`} aria-label="Načítání hlášení">
        <div className={style.tableHead}>
            {Array.from({length: columns}).map((_, index) => (
                <span key={index} className={style.skeletonHeader}></span>
            ))}
        </div>
        {Array.from({length: 4}).map((_, rowIndex) => (
            <div key={rowIndex} className={style.skeletonRow}>
                {Array.from({length: columns}).map((_, columnIndex) => (
                    <span key={columnIndex} className={columnIndex === 0 ? style.skeletonTitle : ""}></span>
                ))}
            </div>
        ))}
    </div>;
}

function Meta({label, value}: {label: string; value: string}) {
    return <div>
        <dt>{label}</dt>
        <dd>{value}</dd>
    </div>;
}

function PersonMeta({label, account}: {label: string; account: ProblemReportItem["reporter"]}) {
    return <div>
        <dt>{label}</dt>
        <dd className={style.person}>
            <Avatar name={account.fullName} size="32px" src={account.avatarUrl} />
            <p>{account.fullName}</p>
        </dd>
    </div>;
}

function formatDate(value: Date) {
    return value.toLocaleString("cs-CZ", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}
