import {useMemo, useState} from "react";
import {Account, AccountGender} from "@/schemas/AccountSchema";
import {accountTypeFilterLabel, accountTypeLabel, genderLabel} from "@/lib/enumLabels";
import {accountTypeOrder} from "@/lib/roles";
import {genderOrder} from "./constants";
import {FilterKey, SchoolOption, SortKey} from "./types";

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

export function useAccountFilters(accounts: Account[]) {
    const [search, setSearch] = useState("");
    const [filters, setFilters] = useState<Record<FilterKey, string[]>>({
        accountType: [],
        gender: [],
        class: [],
        school: [],
        reservations: ["all"],
    });
    const [sort, setSort] = useState<{key: SortKey, direction: "asc" | "desc"}>({key: "fullName", direction: "asc"});

    const schoolOptions = useMemo<SchoolOption[]>(() => {
        const schools = new Map<number, SchoolOption>();

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

    return {
        activeFilterCount,
        changeSort,
        clearFilters,
        filteredAccounts,
        filterOptions,
        filters,
        schoolOptions,
        search,
        setSearch,
        sort,
        toggleFilter,
    };
}
