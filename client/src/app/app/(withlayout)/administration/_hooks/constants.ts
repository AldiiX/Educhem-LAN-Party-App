import {Account, AccountGender} from "@/schemas/AccountSchema";
import {AccountForm} from "./types";

export const emptyAccounts: Account[] = [];
export const genderOrder: AccountGender[] = ["Female", "Male", "Other"];

export const emptyForm: AccountForm = {
    displayName: "",
    firstName: "",
    lastName: "",
    email: "",
    class: "",
    gender: "",
    schoolId: "",
    accountType: "Student",
    avatarUrl: "",
    bannerUrl: "",
    enableReservations: false,
    sendLoginCredentialsEmail: true,
    password: "",
};
