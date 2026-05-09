import style from "./AdministrationFilters.module.scss";
import {FilterKey, FilterOption} from "../_hooks/types";

type AdministrationFiltersProps = {
    activeFilterCount: number;
    filters: Record<FilterKey, string[]>;
    filterOptions: {
        accountType: FilterOption[];
        gender: FilterOption[];
        class: FilterOption[];
        school: FilterOption[];
    };
    hasSearch: boolean;
    onClear: () => void;
    onToggle: (key: FilterKey, value: string) => void;
};

export function AdministrationFilters({activeFilterCount, filters, filterOptions, hasSearch, onClear, onToggle}: AdministrationFiltersProps) {
    return <section className={style.filterPanel}>
        <div className={style.filterHeader}>
            <div>
                <h3>Filtry</h3>
                <p>{activeFilterCount > 0 ? `${activeFilterCount} aktivní` : "Bez omezení"}</p>
            </div>
            <button type="button" onClick={onClear} disabled={activeFilterCount === 0 && !hasSearch}>Vyčistit</button>
        </div>

        <FilterGroup title="Typ účtu" options={filterOptions.accountType} selected={filters.accountType} onToggle={value => onToggle("accountType", value)} />
        <FilterGroup title="Pohlaví" options={filterOptions.gender} selected={filters.gender} onToggle={value => onToggle("gender", value)} />
        <FilterGroup title="Třída" options={filterOptions.class} selected={filters.class} onToggle={value => onToggle("class", value)} />
        <FilterGroup title="Škola" options={filterOptions.school} selected={filters.school} onToggle={value => onToggle("school", value)} />

        <div className={style.filterGroup}>
            <p>Rezervace</p>
            <div className={style.segmented}>
                {[
                    ["all", "Vše"],
                    ["enabled", "Povolené"],
                    ["disabled", "Zakázané"],
                ].map(([value, label]) => (
                    <button key={value} type="button" className={filters.reservations.includes(value) ? style.activeChip : ""} onClick={() => onToggle("reservations", value)}>
                        {label}
                    </button>
                ))}
            </div>
        </div>
    </section>
}

function FilterGroup({title, options, selected, onToggle}: {title: string, options: FilterOption[], selected: string[], onToggle: (value: string) => void}) {
    if(options.length === 0) return null;

    return <div className={style.filterGroup}>
        <p>{title}</p>
        <div className={style.filterChips}>
            {options.map(option => (
                <button key={option.value} type="button" className={selected.includes(option.value) ? style.activeChip : ""} onClick={() => onToggle(option.value)}>
                    <span>{option.label}</span>
                    <small>{option.count}</small>
                </button>
            ))}
        </div>
    </div>
}
