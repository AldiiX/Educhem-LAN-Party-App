import {Account, AccountGender, AccountType} from "@/schemas/AccountSchema";

export type FilterKey = "accountType" | "gender" | "class" | "school" | "reservations";
export type SortKey = "fullName" | "email" | "gender" | "school" | "class" | "accountType" | "createdAtUtc" | "updatedAtUtc" | "lastActiveUtc";
export type ModalMode = "detail" | "edit" | "create" | "delete" | "reset-password";

export type FilterOption = {
    value: string;
    label: string;
    count: number;
};

export type SchoolOption = {
    id: number;
    label: string;
    shortName: string;
    count: number;
};

export type AccountForm = {
    displayName: string;
    firstName: string;
    lastName: string;
    email: string;
    class: string;
    gender: AccountGender | "";
    schoolId: string;
    accountType: AccountType;
    avatarUrl: string;
    bannerUrl: string;
    enableReservations: boolean;
    sendLoginCredentialsEmail: boolean;
    password: string;
};

export type AccountTableSort = {
    key: SortKey;
    direction: "asc" | "desc";
};

export type AccountLike = Account;
