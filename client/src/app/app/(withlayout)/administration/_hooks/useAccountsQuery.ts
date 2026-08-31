import {useMemo} from "react";
import useSWR from "swr";
import {fetcher} from "@/lib/swr";
import {accountTypeFilterLabel, genderLabel} from "@/lib/enumLabels";
import {accountTypeOrder} from "@/lib/roles";
import {
    AdministrationAccountsPage,
    AdministrationAccountsPageSchema,
} from "@/schemas/AdministrationSchema";
import {emptyAccounts, genderOrder} from "./constants";
import {FilterOption, SchoolOption} from "./types";
import type {AccountGender, AccountType} from "@/schemas/AccountSchema";

const accountsFetcher = async (url: string) => {
    return AdministrationAccountsPageSchema.parse(await fetcher<unknown>(url));
};

const emptyPagination = {
    page: 1,
    pageSize: 25,
    totalEntries: 0,
    totalPages: 0,
};

export function useAccountsQuery(queryString: string) {
    const url = `/api/v1/account/all${queryString ? `?${queryString}` : ""}`;
    const {data, error, isLoading, isValidating, mutate} = useSWR<AdministrationAccountsPage>(url, accountsFetcher, {
        keepPreviousData: true,
    });
    const accounts = data?.accounts ?? emptyAccounts;
    const schoolOptions = useMemo<SchoolOption[]>(() => (
        (data?.filterOptions.schools ?? [])
            .map(option => ({
                id: option.school.id,
                label: option.school.displayName,
                shortName: option.school.shortName,
                count: option.count,
            }))
            .sort((a, b) => a.label.localeCompare(b.label, "cs"))
    ), [data?.filterOptions.schools]);
    const filterOptions = useMemo(() => ({
        accountType: (data?.filterOptions.accountTypes ?? [])
            .map<FilterOption>(option => ({
                value: option.value,
                label: accountTypeFilterLabel(option.value),
                count: option.count,
            }))
            .sort((a, b) => accountTypeOrder.indexOf(a.value as AccountType) - accountTypeOrder.indexOf(b.value as AccountType)),
        gender: (data?.filterOptions.genders ?? [])
            .map<FilterOption>(option => ({
                value: option.value,
                label: genderLabel(option.value),
                count: option.count,
            }))
            .sort((a, b) => genderOrder.indexOf(a.value as AccountGender) - genderOrder.indexOf(b.value as AccountGender)),
        class: (data?.filterOptions.classes ?? [])
            .map<FilterOption>(option => ({value: option.value, label: option.value, count: option.count}))
            .sort((a, b) => a.label.localeCompare(b.label, "cs", {numeric: true})),
        school: schoolOptions.map<FilterOption>(school => ({
            value: String(school.id),
            label: school.label.length > 28 ? school.shortName : school.label,
            count: school.count,
        })),
    }), [data?.filterOptions.accountTypes, data?.filterOptions.classes, data?.filterOptions.genders, schoolOptions]);

    const refreshAccounts = async () => (await mutate())?.accounts ?? emptyAccounts;

    return {
        accounts,
        accountsError: error,
        accountsLoading: isLoading,
        accountsValidating: isValidating,
        filterOptions,
        mutateAccounts: mutate,
        pagination: data?.pagination ?? emptyPagination,
        refreshAccounts,
        schoolOptions,
        totalItems: data?.totalItems ?? 0,
    };
}
