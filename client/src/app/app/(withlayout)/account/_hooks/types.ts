import {Dispatch, SetStateAction} from "react";
import {Account, AccountGender} from "@/schemas/AccountSchema";

export type AccountTab = "overview" | "achievements" | "settings";
export type AccountModal = "avatar-info" | "banner-info" | "remove-avatar" | "remove-banner" | null;

export type ProfileDraft = {
    gender: AccountGender | "";
    avatarUrl: string | null;
    bannerUrl: string | null;
};

export type PasswordForm = {
    oldPassword: string;
    newPassword: string;
    newPasswordConfirmation: string;
};

export type PasswordValidations = {
    minLength: boolean;
    lower: boolean;
    upper: boolean;
    number: boolean;
    special: boolean;
};

export type AccountPageState = {
    account: Account;
    selectedTab: AccountTab;
    setSelectedTab: Dispatch<SetStateAction<AccountTab>>;
    modal: AccountModal;
    setModal: Dispatch<SetStateAction<AccountModal>>;
    profileDraft: ProfileDraft;
    setProfileDraft: Dispatch<SetStateAction<ProfileDraft>>;
    passwordForm: PasswordForm;
    setPasswordForm: Dispatch<SetStateAction<PasswordForm>>;
    passwordValidations: PasswordValidations;
    canSubmitPassword: boolean;
    savingProfile: boolean;
    changingPassword: boolean;
    resetProfileDraft: () => void;
    saveProfile: () => Promise<void>;
    changePassword: () => Promise<void>;
    logout: () => Promise<void>;
    toggleTheme: () => void;
    achievementUpdatingIds: ReadonlySet<string>;
    badgeUpdatingIds: ReadonlySet<string>;
    toggleAchievementVisibility: (entryId: string, nextHidden: boolean) => Promise<void>;
    toggleBadgeTakenOut: (entryId: string, nextTakenOut: boolean) => Promise<void>;
};
