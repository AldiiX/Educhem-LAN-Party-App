"use client";

import style from "../layoutclient.module.scss";
import {useAdministrationAccounts} from "@/app/app/(withlayout)/administration/_hooks/useAdministrationAccounts";
import {AdministrationFilters} from "./_components/AdministrationFilters";
import {AccountsTable} from "./_components/AccountsTable";
import {AccountModals} from "./_components/AccountModals";

export function Users() {
    const administration = useAdministrationAccounts();

    if(administration.accountsError) {
        return <>
            <p>Uživatele se nepodařilo načíst.</p>
            <button type="button" onClick={() => administration.mutateAccounts()}>Zkusit znovu</button>
        </>;
    }

    return <>
        <section className={style.toolbar}>
            <div>
                <h2>Uživatelé ({administration.filteredAccounts.length})</h2>
                <p>{administration.accounts.length} celkem</p>
            </div>
            <div className={style.searchBox}>
                <span style={{maskImage: "url(/icons/account.svg)"}}></span>
                <input value={administration.search} onChange={event => administration.setSearch(event.target.value)} placeholder="Hledat uživatele..." />
            </div>
            {administration.canCreateUsers && <button type="button" className={style.addButton} onClick={() => administration.openModal("create")}>+ Přidat uživatele</button>}
        </section>

        <AdministrationFilters
            activeFilterCount={administration.activeFilterCount}
            filters={administration.filters}
            filterOptions={administration.filterOptions}
            hasSearch={administration.search.length > 0}
            onClear={administration.clearFilters}
            onToggle={administration.toggleFilter}
        />

        <AccountsTable
            accounts={administration.filteredAccounts}
            loggedAccountId={administration.loggedAccount?.id}
            sort={administration.sort}
            onSort={administration.changeSort}
            onOpenDetail={account => administration.openModal("detail", account)}
        />

        <AccountModals
            modalMode={administration.modalMode}
            viewerCommunicationStyle={administration.loggedAccount?.communicationStyle}
            selectedAccount={administration.selectedAccount}
            form={administration.form}
            setForm={administration.setForm}
            schoolOptions={administration.schoolOptions}
            manageableAccountTypes={administration.manageableAccountTypes}
            canManageSelectedAccount={administration.canManageSelectedAccount}
            canImpersonateSelectedAccount={administration.canImpersonateSelectedAccount}
            selectedAccountRoleBlocked={administration.selectedAccountRoleBlocked}
            selectedAccountWarningMessage={administration.selectedAccountWarningMessage}
            saving={administration.saving}
            onSubmit={administration.submitAccount}
            onClose={administration.closeModal}
            onOpenDetail={account => administration.openModal("detail", account)}
            onOpenEdit={account => administration.openModal("edit", account)}
            onOpenDelete={account => administration.openModal("delete", account)}
            onOpenResetPassword={account => administration.openModal("reset-password", account)}
            onDelete={administration.deleteAccount}
            onImpersonate={administration.impersonateAccount}
            onResetPassword={administration.resetPassword}
        />
    </>;
}