import style from "./Pagination.module.scss";

type PaginationProps = {
    page: number;
    pageSize: number;
    totalEntries: number;
    totalPages: number;
    loading?: boolean;
    onPageChange: (page: number) => void;
};

export function Pagination({page, pageSize, totalEntries, totalPages, loading = false, onPageChange}: PaginationProps) {
    if(totalEntries === 0) return null;

    const firstEntry = (page - 1) * pageSize + 1;
    const lastEntry = Math.min(page * pageSize, totalEntries);

    return <nav className={style.pagination} aria-label="Stránkování">
        <p className={style.summary}>
            Zobrazeno <strong>{firstEntry}–{lastEntry}</strong> z <strong>{totalEntries}</strong>
        </p>
        <div className={style.controls}>
            <button
                type="button"
                disabled={page <= 1 || loading}
                onClick={() => onPageChange(page - 1)}
            >
                Předchozí
            </button>
            <span>Strana {page} z {Math.max(totalPages, 1)}</span>
            <button
                type="button"
                disabled={page >= totalPages || loading}
                onClick={() => onPageChange(page + 1)}
            >
                Další
            </button>
        </div>
    </nav>;
}
