"use client";

import {type Dispatch, type SetStateAction, useEffect, useState} from "react";

type UseRememberStateOptions<T> = {
    serialize?: (value: T) => string;
    cookieMaxAge?: number;
};

const DEFAULT_COOKIE_MAX_AGE = 31536000;

export function useRememberState<T>(
    key: string,
    initialValue: T,
    options: UseRememberStateOptions<T> = {},
): [T, Dispatch<SetStateAction<T>>] {
    const [value, setValue] = useState(initialValue);
    const serialize = options.serialize ?? String;
    const cookieMaxAge = options.cookieMaxAge ?? DEFAULT_COOKIE_MAX_AGE;

    useEffect(() => {
        const serializedValue = serialize(value);

        try {
            localStorage.setItem(key, serializedValue);
        } catch {
        }

        document.cookie = `${key}=${encodeURIComponent(serializedValue)}; path=/; max-age=${cookieMaxAge}; samesite=lax`;
    }, [cookieMaxAge, key, serialize, value]);

    return [value, setValue];
}
