import style from './LogsToolbar.module.scss' 

type LogsToolbarProps = {
    totalCount: number;
    filteredCount: number;
    searchTerm: string;
    onSearchChange: (value: string) => void;
    onClearFilters: () => void;
    clearDisabled: boolean;
};

export function LogsToolbar({
    totalCount,
    filteredCount,
    searchTerm,
    onSearchChange,
    onClearFilters,
    clearDisabled
}: LogsToolbarProps) {
    return <section className={style.toolbar}>
        <div>
            <h2>Logy ({filteredCount})</h2>
            <p>{totalCount} celkem</p>
        </div>
        <div className={style.searchBox}>
            <span style={{maskImage: "url(/icons/search.svg)"}}></span>
            <input
                type="text"
                placeholder="Hledat v logách..."
                value={searchTerm}
                onChange={(event) => onSearchChange(event.target.value)}
            />
        </div>
        <button
            type="button"
            className={style.clearFiltersButton}
            onClick={onClearFilters}
            disabled={clearDisabled}
        >
            Vyčistit filtry
        </button>
    </section>;
}

