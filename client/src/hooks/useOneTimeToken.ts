"use client";

import {useEffect, useRef, useState} from "react";
import {takeOneTimeTokenFromUrl} from "@/lib/oneTimeToken";

export function useOneTimeToken() {
    const savedToken = useRef<string | null>(null);
    const [token, setToken] = useState<string | null>(null);

    useEffect(() => {
        savedToken.current ??= takeOneTimeTokenFromUrl();
        setToken(savedToken.current);
    }, []);

    return token;
}
