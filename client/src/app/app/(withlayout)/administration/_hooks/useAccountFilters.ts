import {useCallback, useMemo} from "react";
import {AccountTableSort, FilterKey, SortKey} from "./types";
import {readPage, setRepeatedParam, useDebouncedQueryParam, useUrlQueryState} from "./useUrlQueryState";

const sortKeys: SortKey[] = ["fullName", "email", "gender", "school", "class", "accountType", "createdAtUtc", "updatedAtUtc", "lastActiveUtc"];

export function useAccountFilters() {
    const {searchParams, updateQuery} = useUrlQueryState();
    const urlSearch = searchParams.get("q") ?? "";
    const [search, setSearch] = useDebouncedQueryParam("q", urlSearch, updateQuery);
    const page = readPage(searchParams.get("page"));
    const filters = useMemo<Record<FilterKey, string[]>>(() => {
        const reservations = searchParams.get("reservations");

        return {
            accountType: searchParams.getAll("accountType"),
            gender: searchParams.getAll("gender"),
            class: searchParams.getAll("class"),
            school: searchParams.getAll("school"),
            reservations: reservations === "enabled" || reservations === "disabled" ? [reservations] : ["all"],
        };
    }, [searchParams]);
    const sort = useMemo<AccountTableSort>(() => {
        const sortKey = searchParams.get("sort") as SortKey | null;

        return {
            key: sortKey && sortKeys.includes(sortKey) ? sortKey : "fullName",
            direction: searchParams.get("direction") === "desc" ? "desc" : "asc",
        };
    }, [searchParams]);
    const activeFilterCount = filters.accountType.length
        + filters.gender.length
        + filters.class.length
        + filters.school.length
        + (filters.reservations.includes("all") ? 0 : 1);

    const toggleFilter = (key: FilterKey, value: string) => {
        updateQuery(params => {
            if(key === "reservations") {
                if(value === "all") params.delete("reservations");
                else params.set("reservations", value);
                return;
            }

            const current = params.getAll(key);
            setRepeatedParam(
                params,
                key,
                current.includes(value) ? current.filter(item => item !== value) : [...current, value]
            );
        });
    };

    const clearFilters = () => {
        setSearch("");
        updateQuery(params => {
            ["q", "accountType", "gender", "class", "school", "reservations"].forEach(key => params.delete(key));
        });
    };

    const changeSort = (key: SortKey) => {
        updateQuery(params => {
            const direction = sort.key === key && sort.direction === "asc" ? "desc" : "asc";

            if(key === "fullName") params.delete("sort");
            else params.set("sort", key);

            if(direction === "asc") params.delete("direction");
            else params.set("direction", direction);
        });
    };

    const setPage = useCallback((nextPage: number) => {
        updateQuery(params => {
            if(nextPage <= 1) params.delete("page");
            else params.set("page", String(nextPage));
        }, {resetPage: false});
    }, [updateQuery]);

    return {
        activeFilterCount,
        changeSort,
        clearFilters,
        filters,
        page,
        queryString: searchParams.toString(),
        search,
        setPage,
        setSearch,
        sort,
        toggleFilter,
    };
}
