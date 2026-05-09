"use client"

import style from "./client.module.scss";
import {AdministrationFilters} from "./_components/AdministrationFilters";
import {AccountsTable} from "./_components/AccountsTable";
import {AccountModals} from "./_components/AccountModals";
import {useAdministrationAccounts} from "./_hooks/useAdministrationAccounts";

export default function AdministrationClient() {
    const administration = useAdministrationAccounts();

    if(administration.accountsError) {
        return <main className={style.administration}>
            <h1>Administrace</h1>
            <p>Uživatele se nepodařilo načíst.</p>
            <button type="button" onClick={() => administration.mutateAccounts()}>Zkusit znovu</button>
        </main>
    }

    return <main className={style.administration}>
        <h1>Administrace</h1>

        <div className={style.tabs}>
            {["Uživatelé", "Rezervace", "Forum příspěvky", "Bezpečnostní logy", "Nastavení aplikace"].map((tab, index) => (
                <button key={tab} type="button" className={index === 0 ? style.active : ""}>{tab}</button>
            ))}
        </div>

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
            selectedAccount={administration.selectedAccount}
            form={administration.form}
            setForm={administration.setForm}
            schoolOptions={administration.schoolOptions}
            manageableAccountTypes={administration.manageableAccountTypes}
            canManageSelectedAccount={administration.canManageSelectedAccount}
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
            onResetPassword={administration.resetPassword}
        />
    </main>
}
