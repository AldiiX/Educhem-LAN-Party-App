export const fetcher = async <T, >(url: string): Promise<T> => {
    const response = await fetch(url);

    if (! response.ok) {
        throw new Error("Request failed: " + response.statusText);
    }

    return response.json();
};