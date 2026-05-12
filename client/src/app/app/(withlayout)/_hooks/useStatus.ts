import useSWR from "swr";
import {fetcher} from "@/lib/swr";

export type StatusData = {
    maxCapacity: number,
    currentReservations: number,
    capacityUsedPercentage: number,
}

export function useStatus() {
    const {data, error, isLoading, mutate} = useSWR<StatusData>("/api/v1/reservations/status", fetcher);

    const refresh = async () => await mutate();

    return {
        ...data,
        error,
        isLoading,
        refresh,
    };
}
