import {useEffect, useState} from "react";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {phrase} from "@/lib/communicationStyle";
import {accountTypeOrder, canManageAccount, canManageAccountRole, hasRoleAtLeast, isSuperAdmin} from "@/lib/roles";
import {useAccountFilters} from "./useAccountFilters";
import {useAccountModalState} from "./useAccountModalState";
import {useAccountMutations} from "./useAccountMutations";
import {useAccountsQuery} from "./useAccountsQuery";

export function useAdministrationAccounts() {
    const {account: loggedAccount, setAccount: setLoggedAccount} = useAuth();
    const [saving, setSaving] = useState(false);
    const accountFilters = useAccountFilters();
    const accountsQuery = useAccountsQuery(accountFilters.queryString);
    const modalState = useAccountModalState(saving);

    useEffect(() => {
        if(!accountsQuery.accountsValidating && accountsQuery.pagination.page !== accountFilters.page) {
            accountFilters.setPage(accountsQuery.pagination.page);
        }
    }, [accountFilters.page, accountFilters.setPage, accountsQuery.accountsValidating, accountsQuery.pagination.page]);

    const canCreateUsers = hasRoleAtLeast(loggedAccount, "TeacherOrg");
    const selectedAccountSelfBlocked = Boolean(
        loggedAccount
        && modalState.selectedAccount
        && loggedAccount.id === modalState.selectedAccount.id
        && !isSuperAdmin(loggedAccount)
    );
    const selectedAccountRoleBlocked = canCreateUsers && modalState.selectedAccount
        ? !canManageAccount(loggedAccount, modalState.selectedAccount)
        : false;
    const selectedAccountWarningMessage = selectedAccountSelfBlocked
        ? phrase(loggedAccount?.communicationStyle, "Nemůžeš upravit svůj vlastní účet.", "Nemůžete upravit svůj vlastní účet.")
        : "Tohoto uživatele nejde upravit, protože má stejnou nebo vyšší roli.";
    const canManageSelectedAccount = canCreateUsers && modalState.selectedAccount
        ? canManageAccount(loggedAccount, modalState.selectedAccount)
        : false;
    const canImpersonateSelectedAccount = hasRoleAtLeast(loggedAccount, "Admin") && modalState.selectedAccount
        ? canManageAccount(loggedAccount, modalState.selectedAccount)
        : false;
    const manageableAccountTypes = accountTypeOrder.filter(type => canManageAccountRole(loggedAccount, type));

    const accountMutations = useAccountMutations({
        canManageSelectedAccount,
        closeModal: modalState.closeModal,
        form: modalState.form,
        loggedAccount,
        modalMode: modalState.modalMode,
        refreshAccounts: accountsQuery.refreshAccounts,
        saving,
        selectedAccount: modalState.selectedAccount,
        setLoggedAccount,
        setModalMode: modalState.setModalMode,
        setSaving,
        setSelectedAccount: modalState.setSelectedAccount,
    });

    return {
        ...accountsQuery,
        ...accountFilters,
        ...modalState,
        ...accountMutations,
        filteredAccounts: accountsQuery.accounts,
        canCreateUsers,
        canImpersonateSelectedAccount,
        canManageSelectedAccount,
        loggedAccount,
        manageableAccountTypes,
        saving,
        selectedAccountRoleBlocked,
        selectedAccountWarningMessage,
    };
}
