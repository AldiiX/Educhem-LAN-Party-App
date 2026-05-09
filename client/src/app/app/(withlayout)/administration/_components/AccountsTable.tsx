import {Account} from "@/schemas/AccountSchema";
import {Avatar} from "@/components/Avatar";
import {accountTypeLabel, genderLabel, schoolLabel} from "@/lib/enumLabels";
import style from "./AccountsTable.module.scss";
import {AccountTableSort, SortKey} from "../_hooks/types";

type AccountsTableProps = {
    accounts: Account[];
    loggedAccountId?: string;
    sort: AccountTableSort;
    onSort: (key: SortKey) => void;
    onOpenDetail: (account: Account) => void;
};

export function AccountsTable({accounts, loggedAccountId, sort, onSort, onOpenDetail}: AccountsTableProps) {
    return <section className={style.tableWrap}>
        <table>
            <colgroup>
                <col className={style.nameColumn} />
                <col className={style.emailColumn} />
                <col className={style.compactColumn} />
                <col className={style.schoolColumn} />
                <col className={style.compactColumn} />
                <col className={style.typeColumn} />
                <col className={style.dateColumn} />
                <col className={style.dateColumn} />
                <col className={style.dateColumn} />
            </colgroup>
            <thead>
                <tr>
                    <SortableTh label="Jméno a příjmení" sortKey="fullName" sort={sort} onSort={onSort} />
                    <SortableTh label="Email" sortKey="email" sort={sort} onSort={onSort} />
                    <SortableTh label="Pohlaví" sortKey="gender" sort={sort} onSort={onSort} />
                    <SortableTh label="Škola" sortKey="school" sort={sort} onSort={onSort} />
                    <SortableTh label="Třída" sortKey="class" sort={sort} onSort={onSort} />
                    <SortableTh label="Typ účtu" sortKey="accountType" sort={sort} onSort={onSort} />
                    <SortableTh label="Vytvořen" sortKey="createdAtUtc" sort={sort} onSort={onSort} />
                    <SortableTh label="Naposledy upraven" sortKey="updatedAtUtc" sort={sort} onSort={onSort} />
                    <SortableTh label="Naposledy přihlášen" sortKey="lastActiveUtc" sort={sort} onSort={onSort} />
                </tr>
            </thead>
            <tbody>
                {accounts.map(account => (
                    <tr key={account.id} className={loggedAccountId === account.id ? style.currentUser : ""} onClick={() => onOpenDetail(account)}>
                        <td>
                            <div className={style.userCell}>
                                <Avatar name={account.fullName} src={account.avatarUrl} size="28px" />
                                <span>{account.fullName}</span>
                            </div>
                        </td>
                        <td>{account.email}</td>
                        <td>{genderLabel(account.gender)}</td>
                        <td title={account.school?.displayName}>{schoolLabel(account.school)}</td>
                        <td>{account.class}</td>
                        <td>{accountTypeLabel(account.accountType, account.gender)}</td>
                        <td title={formatDate(account.createdAtUtc)}>{formatDate(account.createdAtUtc)}</td>
                        <td title={formatDate(account.updatedAtUtc)}>{formatDate(account.updatedAtUtc)}</td>
                        <td title={formatDate(account.lastActiveUtc)}>{formatDate(account.lastActiveUtc)}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    </section>
}

function SortableTh({label, sortKey, sort, onSort}: {label: string, sortKey: SortKey, sort: AccountTableSort, onSort: (key: SortKey) => void}) {
    const active = sort.key === sortKey;

    return <th>
        <button type="button" onClick={() => onSort(sortKey)} className={active ? style.sorted : ""}>
            {label}
            <span>{active ? (sort.direction === "asc" ? "↑" : "↓") : ""}</span>
        </button>
    </th>
}

function formatDate(value?: Date | null) {
    if(!value) return "";

    return new Intl.DateTimeFormat("cs-CZ", {
        day: "numeric",
        month: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
        second: "2-digit",
    }).format(value);
}
