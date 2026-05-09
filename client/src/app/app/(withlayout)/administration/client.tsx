"use client"

import {FormEvent, useEffect, useMemo, useState} from "react";
import Link from "next/link";
import useSWR from "swr";
import {toast} from "react-hot-toast";
import style from "./client.module.scss";
import {Account, AccountGender, AccountSchema} from "@/schemas/AccountSchema";
import {Avatar} from "@/components/Avatar";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {Modal} from "@/components/Modal";
import {ModalDestructive, ModalInformative} from "@/components/ModalDialog";
import {accountTypeFilterLabel, accountTypeLabel, genderLabel, schoolLabel} from "@/lib/enumLabels";
import {fetcher} from "@/lib/swr";

type AccountType = NonNullable<Account["accountType"]>;
type FilterKey = "accountType" | "gender" | "class" | "school" | "reservations";
type SortKey = "fullName" | "email" | "gender" | "school" | "class" | "accountType" | "createdAtUtc" | "updatedAtUtc" | "lastActiveUtc";
type ModalMode = "detail" | "edit" | "create" | "delete" | "reset-password";

type FilterOption = {
    value: string;
    label: string;
    count: number;
};

type AccountForm = {
    displayName: string;
    firstName: string;
    lastName: string;
    email: string;
    class: string;
    gender: AccountGender | "";
    schoolId: string;
    accountType: AccountType;
    avatarUrl: string;
    bannerUrl: string;
    enableReservations: boolean;
    sendLoginCredentialsEmail: boolean;
    password: string;
};

const accountTypeOrder: AccountType[] = ["Student", "Teacher", "TeacherOrg", "Admin", "SuperAdmin"];
const genderOrder: AccountGender[] = ["Female", "Male", "Other"];
const emptyForm: AccountForm = {
    displayName: "",
    firstName: "",
    lastName: "",
    email: "",
    class: "",
    gender: "",
    schoolId: "",
    accountType: "Student",
    avatarUrl: "",
    bannerUrl: "",
    enableReservations: false,
    sendLoginCredentialsEmail: true,
    password: "",
};

const emptyAccounts: Account[] = [];

const accountsFetcher = async (url: string) => {
    const response = await fetcher<unknown>(url);

    return AccountSchema.array().parse(response ?? []);
};

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

function accountToForm(account: Account | null): AccountForm {
    if(!account) {
        return {...emptyForm };
    }

    return {
        displayName: account.fullName ?? "",
        firstName: account.firstName ?? "",
        lastName: account.lastName ?? "",
        email: account.email ?? "",
        class: account.class ?? "",
        gender: account.gender ?? "",
        schoolId: String(account.school?.id),
        accountType: account.accountType ?? "Student",
        avatarUrl: account.avatarUrl ?? "",
        bannerUrl: account.bannerUrl ?? "",
        enableReservations: Boolean(account.enableReservations),
        sendLoginCredentialsEmail: false,
        password: "",
    };
}

function splitDisplayName(displayName: string) {
    const parts = displayName.trim().split(/\s+/).filter(Boolean);
    const firstName = parts.shift() ?? "";
    const lastName = parts.join(" ");

    return {firstName, lastName};
}

function getSortValue(account: Account, key: SortKey) {
    switch (key) {
        case "school":
            return account.school?.displayName ?? "";
        case "accountType":
            return accountTypeLabel(account.accountType, account.gender);
        case "gender":
            return genderLabel(account.gender);
        default:
            return account[key] ?? "";
    }
}

function uniqueOptions(accounts: Account[], selector: (account: Account) => string | null | undefined, labeler = (value: string) => value, order?: string[]) {
    const counts = new Map<string, number>();

    accounts.forEach(account => {
        const value = selector(account);
        if(!value) return;
        counts.set(value, (counts.get(value) ?? 0) + 1);
    });

    return Array.from(counts.entries())
        .map(([value, count]) => ({value, label: labeler(value), count}))
        .sort((a, b) => {
            if(order) {
                return (order.indexOf(a.value) === -1 ? 999 : order.indexOf(a.value))
                    - (order.indexOf(b.value) === -1 ? 999 : order.indexOf(b.value));
            }

            return a.label.localeCompare(b.label, "cs", {numeric: true});
        });
}

export default function AdministrationClient() {
    const {account: loggedAccount, setAccount: setLoggedAccount} = useAuth();
    const {data: fetchedAccounts, error: accountsError, isLoading: accountsLoading, mutate: mutateAccounts} = useSWR<Account[]>("/api/v1/account/all", accountsFetcher);
    const accounts = fetchedAccounts ?? emptyAccounts;
    const [search, setSearch] = useState("");
    const [filters, setFilters] = useState<Record<FilterKey, string[]>>({
        accountType: [],
        gender: [],
        class: [],
        school: [],
        reservations: ["all"],
    });
    const [sort, setSort] = useState<{key: SortKey, direction: "asc" | "desc"}>({key: "fullName", direction: "asc"});
    const [modalMode, setModalMode] = useState<ModalMode | null>(null);
    const [selectedAccount, setSelectedAccount] = useState<Account | null>(null);
    const [form, setForm] = useState<AccountForm>(emptyForm);
    const [saving, setSaving] = useState(false);

    const schoolOptions = useMemo(() => {
        const schools = new Map<number, {id: number, label: string, shortName: string, count: number}>();

        accounts.forEach(account => {
            if(!account.school) return;

            const current = schools.get(account.school.id);
            schools.set(account.school.id, {
                id: account.school.id,
                label: account.school.displayName,
                shortName: account.school.shortName,
                count: (current?.count ?? 0) + 1,
            });
        });

        return Array.from(schools.values()).sort((a, b) => a.label.localeCompare(b.label, "cs"));
    }, [accounts]);

    const filterOptions = useMemo(() => ({
        accountType: uniqueOptions(accounts, account => account.accountType, accountTypeFilterLabel, accountTypeOrder),
        gender: uniqueOptions(accounts, account => account.gender, value => genderLabel(value as AccountGender), genderOrder),
        class: uniqueOptions(accounts, account => account.class),
        school: schoolOptions.map(school => ({value: String(school.id), label: school.label.length > 28 ? school.shortName : school.label, count: school.count})),
    }), [accounts, schoolOptions]);

    const activeFilterCount = filters.accountType.length + filters.gender.length + filters.class.length + filters.school.length + (filters.reservations.includes("all") ? 0 : 1);

    const filteredAccounts = useMemo(() => {
        const query = search.trim().toLowerCase();

        return accounts
            .filter(account => {
                const matchesSearch = query.length === 0 || [
                    account.fullName,
                    account.email,
                    account.class,
                    account.school?.displayName,
                    accountTypeLabel(account.accountType, account.gender),
                ].some(value => value?.toLowerCase().includes(query));

                const matchesType = filters.accountType.length === 0 || filters.accountType.includes(account.accountType ?? "");
                const matchesGender = filters.gender.length === 0 || filters.gender.includes(account.gender ?? "");
                const matchesClass = filters.class.length === 0 || filters.class.includes(account.class ?? "");
                const matchesSchool = filters.school.length === 0 || filters.school.includes(String(account.school?.id ?? ""));
                const matchesReservations = filters.reservations.includes("all")
                    || (filters.reservations.includes("enabled") && account.enableReservations)
                    || (filters.reservations.includes("disabled") && !account.enableReservations);

                return matchesSearch && matchesType && matchesGender && matchesClass && matchesSchool && matchesReservations;
            })
            .sort((a, b) => {
                const direction = sort.direction === "asc" ? 1 : -1;
                const aValue = getSortValue(a, sort.key);
                const bValue = getSortValue(b, sort.key);

                if(aValue instanceof Date && bValue instanceof Date) {
                    return (aValue.getTime() - bValue.getTime()) * direction;
                }

                return String(aValue).localeCompare(String(bValue), "cs", {numeric: true}) * direction;
            });
    }, [accounts, filters, search, sort]);

    const refreshAccounts = async () => {
        return await mutateAccounts() ?? emptyAccounts;
    };

    const openModal = (mode: ModalMode, account: Account | null = null) => {
        setSelectedAccount(account);
        setForm(accountToForm(account));
        setModalMode(mode);
    };

    const closeModal = () => {
        if(saving) return;
        setModalMode(null);
        setSelectedAccount(null);
        setForm(emptyForm);
    };

    const toggleFilter = (key: FilterKey, value: string) => {
        setFilters(current => {
            if(key === "reservations") {
                return {...current, reservations: [value]};
            }

            return {
                ...current,
                [key]: current[key].includes(value)
                    ? current[key].filter(item => item !== value)
                    : [...current[key], value],
            };
        });
    };

    const clearFilters = () => {
        setFilters({accountType: [], gender: [], class: [], school: [], reservations: ["all"]});
        setSearch("");
    };

    const changeSort = (key: SortKey) => {
        setSort(current => ({
            key,
            direction: current.key === key && current.direction === "asc" ? "desc" : "asc",
        }));
    };

    const submitAccount = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if(saving) return;

        const {firstName, lastName} = splitDisplayName(form.displayName);

        if(firstName.length < 2 || lastName.length < 2) {
            toast.error("Jméno i příjmení musí mít alespoň 2 znaky.");
            return;
        }

        if(form.email.trim().length < 5) {
            toast.error("Email musí mít alespoň 5 znaků.");
            return;
        }

        setSaving(true);

        const body = {
            firstName,
            lastName,
            email: form.email,
            class: form.class,
            gender: form.gender || null,
            schoolId: form.schoolId ? Number(form.schoolId) : null,
            accountType: form.accountType,
            avatarUrl: form.avatarUrl,
            bannerUrl: form.bannerUrl,
            enableReservations: form.enableReservations,
            sendLoginCredentialsEmail: form.sendLoginCredentialsEmail,
            password: form.password,
        };

        try {
            const response = await fetch(modalMode === "create" ? "/api/v1/account" : `/api/v1/account/${selectedAccount?.id}`, {
                method: modalMode === "create" ? "POST" : "PUT",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify(body),
            });

            if(!response.ok) throw new Error("Save failed");

            const data = await response.json() as {account: unknown, loginCredentialsEmailSent?: boolean};
            const savedAccount = AccountSchema.parse(data.account);
            const refreshed = await refreshAccounts();
            const nextSelected = refreshed.find(account => account.id === savedAccount.id) ?? savedAccount;

            if(loggedAccount?.id === nextSelected.id) {
                setLoggedAccount(nextSelected);
            }

            setSelectedAccount(nextSelected);
            setModalMode("detail");
            toast.success(modalMode === "create" ? `Uživatel ${nextSelected.fullName} vytvořen.` : `Uživatel ${nextSelected.fullName} upraven.`);
            if(form.sendLoginCredentialsEmail) {
                if(data.loginCredentialsEmailSent) {
                    toast.success("Přihlašovací údaje byly odeslány na email.");
                } else {
                    toast.error("Uživatel je uložený, ale email se nepodařilo odeslat.");
                }
            }
        } catch {
            toast.error("Změny se nepodařilo uložit.");
        } finally {
            setSaving(false);
        }
    };

    const deleteAccount = async () => {
        if(!selectedAccount || saving) return;

        setSaving(true);
        try {
            const response = await fetch(`/api/v1/account/${selectedAccount.id}`, {method: "DELETE"});
            if(!response.ok) throw new Error("Delete failed");

            await refreshAccounts();
            closeModal();
            toast.success("Uživatel smazán.");
        } catch {
            toast.error("Uživatele se nepodařilo smazat.");
        } finally {
            setSaving(false);
        }
    };

    const resetPassword = async () => {
        if(!selectedAccount || saving) return;

        setSaving(true);
        try {
            const response = await fetch(`/api/v1/account/${selectedAccount.id}/reset-password`, {method: "POST"});
            if(!response.ok) throw new Error("Reset failed");

            const data = await response.json() as {loginCredentialsEmailSent?: boolean};
            setModalMode("detail");
            toast.success(data.loginCredentialsEmailSent ? "Heslo resetováno a odesláno na email." : "Heslo resetováno, email se nepodařilo odeslat.");
        } catch {
            toast.error("Heslo se nepodařilo resetovat.");
        } finally {
            setSaving(false);
        }
    };

    useEffect(() => {
        if(!selectedAccount) return;

        const nextSelectedAccount = accounts.find(account => account.id === selectedAccount.id);
        if(nextSelectedAccount) {
            setSelectedAccount(nextSelectedAccount);
        }
    }, [accounts, selectedAccount?.id]);

    const canMutateUsers = loggedAccount?.accountType === "Admin" || loggedAccount?.accountType === "SuperAdmin";

    /*if(accountsLoading) {
        return <main className={style.administration}>
            <h1>Administrace</h1>
            <p>Načítám uživatele...</p>
        </main>
    }*/

    if(accountsError) {
        return <main className={style.administration}>
            <h1>Administrace</h1>
            <p>Uživatele se nepodařilo načíst.</p>
            <button type="button" onClick={() => mutateAccounts()}>Zkusit znovu</button>
        </main>
    }

    return <main className={style.administration}>
        <h1>Administrace</h1>

        <div className={style.tabs}>
            {["Uživatelé", "Rezervace", "Forum příspěvky", "Bezpečnostní logy", "Nastavení aplikace"].map((tab, index) => (
                <button key={tab} type="button" className={index === 0 ? style.active : ""}>{tab}</button>
            ))}
        </div>

        <section className={style.toolbar}>
            <div>
                <h2>Uživatelé ({filteredAccounts.length})</h2>
                <p>{accounts.length} celkem</p>
            </div>
            <div className={style.searchBox}>
                <span style={{maskImage: "url(/icons/account.svg)"}}></span>
                <input value={search} onChange={event => setSearch(event.target.value)} placeholder="Hledat uživatele..." />
            </div>
            {canMutateUsers && <button type="button" className={style.addButton} onClick={() => openModal("create")}>+ Přidat uživatele</button>}
        </section>

        <section className={style.filterPanel}>
            <div className={style.filterHeader}>
                <div>
                    <h3>Filtry</h3>
                    <p>{activeFilterCount > 0 ? `${activeFilterCount} aktivní` : "Bez omezení"}</p>
                </div>
                <button type="button" onClick={clearFilters} disabled={activeFilterCount === 0 && !search}>Vyčistit</button>
            </div>

            <FilterGroup title="Typ účtu" options={filterOptions.accountType} selected={filters.accountType} onToggle={value => toggleFilter("accountType", value)} />
            <FilterGroup title="Pohlaví" options={filterOptions.gender} selected={filters.gender} onToggle={value => toggleFilter("gender", value)} />
            <FilterGroup title="Třída" options={filterOptions.class} selected={filters.class} onToggle={value => toggleFilter("class", value)} />
            <FilterGroup title="Škola" options={filterOptions.school} selected={filters.school} onToggle={value => toggleFilter("school", value)} />

            <div className={style.filterGroup}>
                <p>Rezervace</p>
                <div className={style.segmented}>
                    {[
                        ["all", "Vše"],
                        ["enabled", "Povolené"],
                        ["disabled", "Zakázané"],
                    ].map(([value, label]) => (
                        <button key={value} type="button" className={filters.reservations.includes(value) ? style.activeChip : ""} onClick={() => toggleFilter("reservations", value)}>
                            {label}
                        </button>
                    ))}
                </div>
            </div>
        </section>

        <section className={style.tableWrap}>
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
                        <SortableTh label="Jméno a příjmení" sortKey="fullName" sort={sort} onSort={changeSort} />
                        <SortableTh label="Email" sortKey="email" sort={sort} onSort={changeSort} />
                        <SortableTh label="Pohlaví" sortKey="gender" sort={sort} onSort={changeSort} />
                        <SortableTh label="Škola" sortKey="school" sort={sort} onSort={changeSort} />
                        <SortableTh label="Třída" sortKey="class" sort={sort} onSort={changeSort} />
                        <SortableTh label="Typ účtu" sortKey="accountType" sort={sort} onSort={changeSort} />
                        <SortableTh label="Vytvořen" sortKey="createdAtUtc" sort={sort} onSort={changeSort} />
                        <SortableTh label="Naposledy upraven" sortKey="updatedAtUtc" sort={sort} onSort={changeSort} />
                        <SortableTh label="Naposledy přihlášen" sortKey="lastActiveUtc" sort={sort} onSort={changeSort} />
                    </tr>
                </thead>
                <tbody>
                    {filteredAccounts.map(account => (
                        <tr key={account.id} className={loggedAccount?.id === account.id ? style.currentUser : ""} onClick={() => openModal("detail", account)}>
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

        <Modal open={modalMode === "detail" || modalMode === "edit" || modalMode === "create"} onClose={closeModal} className={style.userModal}>
            {modalMode === "edit" || modalMode === "create" ? (
                <AccountFormModal
                    mode={modalMode}
                    form={form}
                    setForm={setForm}
                    schoolOptions={schoolOptions}
                    saving={saving}
                    onSubmit={submitAccount}
                    onCancel={() => selectedAccount ? openModal("detail", selectedAccount) : closeModal()}
                    previewAccount={selectedAccount}
                />
            ) : selectedAccount ? (
                <AccountDetailModal
                    account={selectedAccount}
                    canMutate={canMutateUsers}
                    onEdit={() => openModal("edit", selectedAccount)}
                    onDelete={() => openModal("delete", selectedAccount)}
                    onResetPassword={() => openModal("reset-password", selectedAccount)}
                />
            ) : null}
        </Modal>

        <ModalDestructive
            open={modalMode === "delete" && selectedAccount !== null}
            title="Smazat uživatele"
            description={selectedAccount ? `Opravdu chceš smazat účet ${selectedAccount.fullName}? Tahle akce nejde vrátit.` : ""}
            confirmText="Smazat"
            loading={saving}
            onClose={() => selectedAccount ? openModal("detail", selectedAccount) : closeModal()}
            onConfirm={deleteAccount}
        />

        <ModalInformative
            open={modalMode === "reset-password" && selectedAccount !== null}
            title="Resetovat heslo"
            description={selectedAccount ? `Vygeneruje se nové heslo pro účet ${selectedAccount.fullName}. Heslo platí, dokud si ho uživatel nezmění.` : ""}
            confirmText="Resetovat"
            loading={saving}
            onClose={() => selectedAccount ? openModal("detail", selectedAccount) : closeModal()}
            onConfirm={resetPassword}
        />
    </main>
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

function SortableTh({label, sortKey, sort, onSort}: {label: string, sortKey: SortKey, sort: {key: SortKey, direction: "asc" | "desc"}, onSort: (key: SortKey) => void}) {
    const active = sort.key === sortKey;

    return <th>
        <button type="button" onClick={() => onSort(sortKey)} className={active ? style.sorted : ""}>
            {label}
            <span>{active ? (sort.direction === "asc" ? "↑" : "↓") : ""}</span>
        </button>
    </th>
}

function AccountDetailModal({account, canMutate, onEdit, onDelete, onResetPassword}: {
    account: Account,
    canMutate: boolean,
    onEdit: () => void,
    onDelete: () => void,
    onResetPassword: () => void,
}) {
    return <>
        <div className={style.modalTop}>
            <div className={account.bannerUrl ? style.userdefinedBanner : style.generatedBanner} style={{
                backgroundImage: account.bannerUrl ? `url(${account.bannerUrl})` : account.avatarUrl ? `url(${account.avatarUrl})` : undefined,
            }}></div>
            <Avatar name={account.fullName} src={account.avatarUrl} size="200px" className={style.modalAvatar} />
        </div>
        <div className={style.modalBottom}>
            <h2>{account.fullName}</h2>
            <p className={account.enableReservations ? style.enabled : style.disabled}>{account.enableReservations ? "Rezervace povolené" : "Rezervace zakázané"}</p>

            <div className={style.profileActions}>
                {canMutate && <button type="button" onClick={onEdit}>Upravit</button>}
                <Link href={`/app/profile/${account.id}`}>
                    <span style={{maskImage: "url(/icons/account.svg)"}}></span>
                    Veřejný profil
                </Link>
            </div>

            <div className={style.infoRows}>
                <InfoRow icon="/icons/email.svg">{account.email}</InfoRow>
                <InfoRow icon="/icons/class.svg">{account.class || "-"}</InfoRow>
                <InfoRow icon="/icons/gender.svg">{genderLabel(account.gender)}</InfoRow>
                <InfoRow icon="/icons/user_with_shield.svg">{accountTypeLabel(account.accountType, account.gender)}</InfoRow>
                <InfoRow icon="/icons/organization.svg" title={account.school?.displayName}>{schoolLabel(account.school) || "-" }</InfoRow>
            </div>

            {canMutate && <div className={style.modalActions}>
                <button type="button" className={style.dangerTextButton} onClick={onResetPassword}>
                    <span style={{maskImage: "url(/icons/reset_password.svg)"}}></span>
                    Resetovat heslo
                </button>
                <button type="button" className={style.dangerButton} onClick={onDelete}>Smazat</button>
            </div>}
        </div>
    </>
}

function AccountFormModal({mode, form, setForm, schoolOptions, saving, onSubmit, onCancel, previewAccount}: {
    mode: "edit" | "create",
    form: AccountForm,
    setForm: (form: AccountForm) => void,
    schoolOptions: {id: number, label: string, shortName: string}[],
    saving: boolean,
    onSubmit: (event: FormEvent<HTMLFormElement>) => void,
    onCancel: () => void,
    previewAccount: Account | null,
}) {
    const update = <K extends keyof AccountForm>(key: K, value: AccountForm[K]) => setForm({...form, [key]: value});
    const previewName = form.displayName.trim() || "?";

    return <form onSubmit={onSubmit}>
        <div className={style.modalTop}>
            <div className={form.bannerUrl ? style.userdefinedBanner : style.generatedBanner} style={{
                backgroundImage: form.bannerUrl ? `url(${form.bannerUrl})` : form.avatarUrl ? `url(${form.avatarUrl})` : previewAccount?.bannerUrl ? `url(${previewAccount.bannerUrl})` : undefined,
            }}></div>
            <Avatar name={previewName} src={form.avatarUrl || previewAccount?.avatarUrl} size="200px" className={style.modalAvatar} />
        </div>
        <div className={style.modalBottom}>
            <input className={style.nameInput} value={form.displayName} onChange={event => update("displayName", event.target.value)} placeholder={mode === "create" ? "Jméno" : "Jméno uživatele"} />

            <div className={style.formButtons}>
                <button type="submit" disabled={saving}>{saving ? "Ukládám..." : "Uložit změny"}</button>
                <button type="button" onClick={onCancel}>Zrušit změny</button>
            </div>

            <div className={style.editRows}>
                <EditRow icon="/icons/email.svg">
                    <input type="email" value={form.email} onChange={event => update("email", event.target.value)} placeholder="Email" />
                </EditRow>
                <EditRow icon="/icons/class.svg">
                    <input value={form.class} onChange={event => update("class", event.target.value)} placeholder="Třída" />
                </EditRow>
                <EditRow icon="/icons/gender.svg">
                    <select value={form.gender} onChange={event => update("gender", event.target.value as AccountGender | "")}>
                        <option value="">Nezadáno</option>
                        <option value="Female">Žena</option>
                        <option value="Male">Muž</option>
                        <option value="Other">Ostatní</option>
                    </select>
                </EditRow>
                <EditRow icon="/icons/user_with_shield.svg">
                    <select value={form.accountType} onChange={event => update("accountType", event.target.value as AccountType)}>
                        {accountTypeOrder.map(type => <option key={type} value={type}>{accountTypeFilterLabel(type)}</option>)}
                    </select>
                </EditRow>
                <EditRow icon="/icons/organization.svg">
                    <select value={form.schoolId} onChange={event => update("schoolId", event.target.value)}>
                        <option value="">Nezadáno</option>
                        {schoolOptions.map(school => <option key={school.id} value={school.id}>{school.label.length > 28 ? school.shortName : school.label}</option>)}
                    </select>
                </EditRow>
                <EditRow icon="/icons/account.svg">
                    <input value={form.avatarUrl} onChange={event => update("avatarUrl", event.target.value)} placeholder="Avatar URL" />
                </EditRow>
                <EditRow icon="/icons/map.svg">
                    <input value={form.bannerUrl} onChange={event => update("bannerUrl", event.target.value)} placeholder="Banner URL" />
                </EditRow>
                <EditRow icon="/icons/reset_password.svg">
                    <input type="text" value={form.password} onChange={event => update("password", event.target.value)} placeholder={mode === "create" ? "Heslo, prázdné = generovat" : "Nové heslo"} />
                </EditRow>
            </div>

            <div className={style.separator}></div>

            <label className={style.toggleRow}>
                <span>Povolit rezervace</span>
                <input type="checkbox" checked={form.enableReservations} onChange={event => update("enableReservations", event.target.checked)} />
            </label>

            <label className={style.toggleRow}>
                <span>Odeslat přihlašovací údaje na email</span>
                <input type="checkbox" checked={form.sendLoginCredentialsEmail} onChange={event => update("sendLoginCredentialsEmail", event.target.checked)} />
            </label>
        </div>
    </form>
}

function InfoRow({icon, children, title}: {icon: string, children?: React.ReactNode, title?: string}) {
    return <div className={style.infoRow}>
        <span className={style.rowIcon} style={{maskImage: `url(${icon})`}}></span>
        <p title={title}>{children}</p>
    </div>
}

function EditRow({icon, children}: {icon: string, children: React.ReactNode}) {
    return <label className={style.editRow}>
        <span className={style.rowIcon} style={{maskImage: `url(${icon})`}}></span>
        {children}
    </label>
}
