import style from "./AdministrationFilters.module.scss";
import {FilterKey, FilterOption} from "../../_hooks/types";
import {CollapsibleFilterPanel} from "../../_components/CollapsibleFilterPanel";

type AdministrationFiltersProps = {
    activeFilterCount: number;
    collapsed: boolean;
    filters: Record<FilterKey, string[]>;
    filterOptions: {
        accountType: FilterOption[];
        gender: FilterOption[];
        class: FilterOption[];
        school: FilterOption[];
    };
    hasSearch: boolean;
    onClear: () => void;
    onCollapseToggle: () => void;
    onToggle: (key: FilterKey, value: string) => void;
};

export function AdministrationFilters({activeFilterCount, collapsed, filters, filterOptions, hasSearch, onClear, onCollapseToggle, onToggle}: AdministrationFiltersProps) {
    return <CollapsibleFilterPanel
        activeFilterCount={activeFilterCount}
        collapsed={collapsed}
        contentId="administration-users-filters"
        hasSearch={hasSearch}
        onClear={onClear}
        onToggle={onCollapseToggle}
    >
        <div className={style.filterGrid}>
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
        </div>
    </CollapsibleFilterPanel>
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
