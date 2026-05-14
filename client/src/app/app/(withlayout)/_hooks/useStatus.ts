import useSWR from "swr";
import { fetcher } from "@/lib/swr";

export type StatusData = {
    maxCapacity: number;
    capacityUsed: number;
    capacityUsedPercentage: number;
    accountsWithEnabledReservations: number;
    accountsWithEnabledReservationsPercentage: number;
}

export function useStatus(initialValue?: StatusData | null) {
    const hasInitialValue = initialValue != null;

    const { data, error, isLoading, isValidating, mutate } = useSWR<StatusData>(
        "/api/v1/reservations/status",
        fetcher,
        {
            fallbackData: initialValue ?? undefined,
            revalidateOnMount: !hasInitialValue,
            revalidateIfStale: !hasInitialValue
        }
    );

    const refresh = async () => await mutate();

    return {
        data: data ?? null,
        error,
        isLoading: !hasInitialValue && isLoading,
        isValidating,
        refresh,
        mutate
    };
}