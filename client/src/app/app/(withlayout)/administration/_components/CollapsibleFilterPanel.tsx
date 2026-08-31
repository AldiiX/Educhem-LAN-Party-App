import {type ReactNode} from "react";
import style from "./CollapsibleFilterPanel.module.scss";

type CollapsibleFilterPanelProps = {
    activeFilterCount: number;
    children: ReactNode;
    collapsed: boolean;
    contentId: string;
    hasSearch?: boolean;
    onClear?: () => void;
    onToggle: () => void;
};

export function CollapsibleFilterPanel({
    activeFilterCount,
    children,
    collapsed,
    contentId,
    hasSearch = false,
    onClear,
    onToggle,
}: CollapsibleFilterPanelProps) {
    return <section className={`${style.panel} ${collapsed ? style.collapsed : ""}`}>
        <div className={style.header}>
            <div className={style.heading}>
                <h3>Filtry</h3>
                <p>{activeFilterCount > 0 ? `${activeFilterCount} aktivní` : "Žádné"}</p>
            </div>

            <div className={style.actions}>
                {onClear && (
                    <button
                        type="button"
                        className={style.clearButton}
                        onClick={onClear}
                        disabled={activeFilterCount === 0 && !hasSearch}
                    >
                        Vyčistit
                    </button>
                )}
                <button
                    type="button"
                    className={style.toggleButton}
                    onClick={onToggle}
                    aria-expanded={!collapsed}
                    aria-controls={contentId}
                >
                    <span>{collapsed ? "Rozbalit" : "Sbalit"}</span>
                    <i aria-hidden="true"></i>
                </button>
            </div>
        </div>

        <div
            id={contentId}
            className={style.content}
            aria-hidden={collapsed}
            inert={collapsed}
        >
            <div className={style.contentInner}>{children}</div>
        </div>
    </section>;
}
