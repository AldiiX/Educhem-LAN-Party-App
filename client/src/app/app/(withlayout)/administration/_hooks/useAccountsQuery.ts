import useSWR from "swr";
import {Account, AccountSchema} from "@/schemas/AccountSchema";
import {fetcher} from "@/lib/swr";
import {emptyAccounts} from "./constants";

const accountsFetcher = async (url: string) => {
    const response = await fetcher<unknown>(url);

    return AccountSchema.array().parse(response ?? []);
};

export function useAccountsQuery() {
    const {data, error, isLoading, mutate} = useSWR<Account[]>("/api/v1/account/all", accountsFetcher);
    const accounts = data ?? emptyAccounts;

    const refreshAccounts = async () => await mutate() ?? emptyAccounts;

    return {
        accounts,
        accountsError: error,
        accountsLoading: isLoading,
        mutateAccounts: mutate,
        refreshAccounts,
    };
}
