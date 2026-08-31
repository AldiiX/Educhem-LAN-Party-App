import {apiFetch} from "@/lib/apiClient";

export const fetcher = async <T, >(url: string): Promise<T> => {
    const response = await apiFetch(url);

    if (! response.ok) {
        throw new Error("Request failed: " + response.statusText);
    }

    return response.json();
};
