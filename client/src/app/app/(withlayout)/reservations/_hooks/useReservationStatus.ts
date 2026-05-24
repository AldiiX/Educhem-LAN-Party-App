import useSWR from "swr";
import {fetcher} from "@/lib/swr";
import {ReservationStatus, ReservationStatusSchema} from "@/schemas/ReservationStatusSchema";

const reservationStatusFetcher = async (url: string) => {
    const response = await fetcher<unknown>(url);

    return ReservationStatusSchema.parse(response);
};

export function useReservationStatus() {
    const {data, error, isLoading, mutate} = useSWR<ReservationStatus>(
        "/api/v1/reservations/status",
        reservationStatusFetcher,
    );

    return {
        reservationStatus: data ?? null,
        reservationStatusError: error,
        reservationStatusLoading: isLoading,
        mutateReservationStatus: mutate,
    };
}
