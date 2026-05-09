import {Account} from "@/schemas/AccountSchema";
import {AccountForm} from "./types";
import {emptyForm} from "./constants";

export function accountToForm(account: Account | null): AccountForm {
    if(!account) return {...emptyForm};

    return {
        displayName: account.fullName ?? "",
        firstName: account.firstName ?? "",
        lastName: account.lastName ?? "",
        email: account.email ?? "",
        class: account.class ?? "",
        gender: account.gender ?? "",
        schoolId: account.school ? String(account.school.id) : "",
        accountType: account.accountType ?? "Student",
        avatarUrl: account.avatarUrl ?? "",
        bannerUrl: account.bannerUrl ?? "",
        enableReservations: Boolean(account.enableReservations),
        sendLoginCredentialsEmail: false,
        password: "",
    };
}

export function splitDisplayName(displayName: string) {
    const parts = displayName.trim().split(/\s+/).filter(Boolean);
    const firstName = parts.shift() ?? "";
    const lastName = parts.join(" ");

    return {firstName, lastName};
}
