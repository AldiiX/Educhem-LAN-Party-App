import {useCallback, useEffect, useState} from "react";
import {usePathname, useRouter, useSearchParams} from "next/navigation";

type UpdateQueryOptions = {
    resetPage?: boolean;
};

export function useUrlQueryState() {
    const pathname = usePathname();
    const router = useRouter();
    const searchParams = useSearchParams();

    const updateQuery = useCallback((update: (params: URLSearchParams) => void, options: UpdateQueryOptions = {}) => {
        const params = new URLSearchParams(searchParams.toString());
        update(params);
        if(options.resetPage !== false) params.delete("page");

        const query = params.toString();
        router.replace(query ? `${pathname}?${query}` : pathname, {scroll: false});
    }, [pathname, router, searchParams]);

    return {searchParams, updateQuery};
}

export function useDebouncedQueryParam(
    key: string,
    urlValue: string,
    updateQuery: (update: (params: URLSearchParams) => void, options?: UpdateQueryOptions) => void,
    delay = 350
) {
    const [value, setValue] = useState(urlValue);

    useEffect(() => {
        setValue(urlValue);
    }, [urlValue]);

    useEffect(() => {
        if(value === urlValue) return;

        const timeout = window.setTimeout(() => {
            updateQuery(params => {
                if(value.trim()) params.set(key, value);
                else params.delete(key);
            });
        }, delay);

        return () => window.clearTimeout(timeout);
    }, [delay, key, updateQuery, urlValue, value]);

    return [value, setValue] as const;
}

export function readPage(value: string | null) {
    const parsed = Number(value);
    return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : 1;
}

export function setRepeatedParam(params: URLSearchParams, key: string, values: string[]) {
    params.delete(key);
    values.forEach(value => params.append(key, value));
}
