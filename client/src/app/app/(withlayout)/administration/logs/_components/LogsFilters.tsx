import style from "./LogsFilter.module.scss";

type LogFiltersProps = {
    logTypes: string[];
    exactTypes: string[];
    selectedLogTypes: Set<string>;
    selectedExactTypes: Set<string>;
    logTypeCounts: Map<string, number>;
    exactTypeCounts: Map<string, number>;
    dateFrom: string;
    dateTo: string;
    actorIdFilter: string;
    targetIdFilter: string;
    onToggleLogType: (type: string) => void;
    onToggleExactType: (type: string) => void;
    onDateFromChange: (value: string) => void;
    onDateToChange: (value: string) => void;
    onActorIdChange: (value: string) => void;
    onTargetIdChange: (value: string) => void;
};

export function LogsFilters({
    logTypes,
    exactTypes,
    selectedLogTypes,
    selectedExactTypes,
    logTypeCounts,
    exactTypeCounts,
    dateFrom,
    dateTo,
    actorIdFilter,
    targetIdFilter,
    onToggleLogType,
    onToggleExactType,
    onDateFromChange,
    onDateToChange,
    onActorIdChange,
    onTargetIdChange,
}: LogFiltersProps) {
    return <section className={style.logsFilters}>
        <div className={style.logsFilterGroup}>
            <p className={style.logsFilterLabel}>Typ logu:</p>
            <div className={style.logsCheckboxes}>
                {logTypes.map((type) => (
                    <label
                        key={type}
                        className={`${style.logsCheckbox} ${selectedLogTypes.has(type) ? style.activeLogChip : ""}`}
                    >
                        <input
                            type="checkbox"
                            checked={selectedLogTypes.has(type)}
                            onChange={() => onToggleLogType(type)}
                        />
                        <span>{type}</span>
                        <small>{logTypeCounts.get(type) ?? 0}</small>
                    </label>
                ))}
            </div>
        </div>

        <div className={style.logsFilterGroup}>
            <p className={style.logsFilterLabel}>Přesný typ:</p>
            <div className={style.logsCheckboxes}>
                {exactTypes.map((type) => (
                    <label
                        key={type}
                        className={`${style.logsCheckbox} ${selectedExactTypes.has(type) ? style.activeLogChip : ""}`}
                    >
                        <input
                            type="checkbox"
                            checked={selectedExactTypes.has(type)}
                            onChange={() => onToggleExactType(type)}
                        />
                        <span>{type}</span>
                        <small>{exactTypeCounts.get(type) ?? 0}</small>
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
                        onChange={(event) => onDateFromChange(event.target.value)}
                    />
                </label>
                <label className={style.logsDateField}>
                    <span>Do:</span>
                    <input
                        type="datetime-local"
                        value={dateTo}
                        onChange={(event) => onDateToChange(event.target.value)}
                    />
                </label>
            </div>
        </div>

        <div className={style.logsFilterGroup}>
            <p className={style.logsFilterLabel}>Aktér a Cíl:</p>
            <div className={style.logsDateFilters}>
                <label className={style.logsDateField}>
                    <span>Aktér:</span>
                    <input
                        type="text"
                        placeholder="UUID aktéra..."
                        value={actorIdFilter}
                        onChange={(event) => onActorIdChange(event.target.value)}
                    />
                </label>
                <label className={style.logsDateField}>
                    <span>Cíl:</span>
                    <input
                        type="text"
                        placeholder="ID cíle..."
                        value={targetIdFilter}
                        onChange={(event) => onTargetIdChange(event.target.value)}
                    />
                </label>
            </div>
        </div>
    </section>;
}