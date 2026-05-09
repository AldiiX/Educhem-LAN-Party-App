import {Account, AccountType} from "@/schemas/AccountSchema";

export const accountTypeRank: Record<AccountType, number> = {
    Student: 0,
    Teacher: 1,
    TeacherOrg: 2,
    Admin: 3,
    SuperAdmin: 4,
};

export const accountTypeOrder = Object.entries(accountTypeRank)
    .sort(([, a], [, b]) => a - b)
    .map(([type]) => type as AccountType);

export function getAccountRoleRank(type?: AccountType | null) {
    return type ? accountTypeRank[type] : accountTypeRank.Student;
}

export function hasRoleAtLeast(account: Pick<Account, "accountType"> | null | undefined, role: AccountType) {
    if(!account?.accountType) return false;

    return getAccountRoleRank(account.accountType) >= getAccountRoleRank(role);
}

export function isSuperAdmin(account: Pick<Account, "accountType"> | null | undefined) {
    return account?.accountType === "SuperAdmin";
}

export function canManageAccountRole(actor: Pick<Account, "accountType"> | null | undefined, targetRole?: AccountType | null) {
    if(!actor?.accountType) return false;
    if(isSuperAdmin(actor)) return true;

    return getAccountRoleRank(actor.accountType) > getAccountRoleRank(targetRole);
}

export function canManageAccount(actor: Pick<Account, "accountType"> | null | undefined, target: Pick<Account, "accountType"> | null | undefined) {
    if(!target) return false;

    return canManageAccountRole(actor, target.accountType);
}
