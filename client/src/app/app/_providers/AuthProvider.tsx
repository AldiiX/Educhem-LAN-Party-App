"use client";

import React, {createContext, type ReactNode, useContext, useMemo, useState} from "react";
import {Account} from "@/schemas/AccountSchema";

type AuthContextValue = {
    account: Account | null;
    setAccount: (user: Account | null) => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({initialUser, children}: { initialUser: Account | null; children: ReactNode }) {
    const [account, setAccount] = useState<Account | null>(initialUser);

    const value = useMemo(() => ({account: account, setAccount: setAccount}), [account]);
    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
    const ctx = useContext(AuthContext);
    if (! ctx) {
        throw new Error("useAuth must be used inside AuthProvider");
    }
    return ctx;
}
