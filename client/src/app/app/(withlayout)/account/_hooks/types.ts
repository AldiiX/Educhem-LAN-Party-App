import {Dispatch, SetStateAction} from "react";
import {Account, AccountCommunicationStyle, AccountGender, AvatarSyncPlatform} from "@/schemas/AccountSchema";
import {ConnectablePlatform} from "@/data/platforms";

export type AccountModal = "avatar-info" | "banner-info" | "remove-avatar" | "remove-banner" | "platform-error" | null;

export type ProfileDraft = {
    gender: AccountGender | "";
    communicationStyle: AccountCommunicationStyle;
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
    differentFromOld: boolean;
};

export type AccountPageState = {
    account: Account;
    modal: AccountModal;
    setModal: Dispatch<SetStateAction<AccountModal>>;
    platformErrorMessage: string | null;
    showPlatformError: (message: string) => void;
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
    platformLoading: boolean;
    connectPlatform: (platform: ConnectablePlatform) => void;
    disconnectPlatform: (platform: ConnectablePlatform) => Promise<void>;
	setAvatarSyncPlatform: (platform: AvatarSyncPlatform | null) => Promise<void>;
};
