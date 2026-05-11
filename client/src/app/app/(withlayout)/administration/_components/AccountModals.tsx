import {Dispatch, FormEvent, ReactNode, SetStateAction} from "react";
import Link from "next/link";
import {Account, AccountGender, AccountType} from "@/schemas/AccountSchema";
import {Avatar} from "@/components/Avatar";
import {Modal} from "@/components/Modal";
import {ModalDestructive, ModalInformative} from "@/components/ModalDialog";
import {accountTypeFilterLabel, accountTypeLabel, genderLabel, schoolLabel} from "@/lib/enumLabels";
import {AccountForm, ModalMode, SchoolOption} from "../_hooks/types";
import style from "./AccountModals.module.scss";

type AccountModalsProps = {
    modalMode: ModalMode | null;
    selectedAccount: Account | null;
    form: AccountForm;
    setForm: Dispatch<SetStateAction<AccountForm>>;
    schoolOptions: SchoolOption[];
    manageableAccountTypes: AccountType[];
    canManageSelectedAccount: boolean;
    canImpersonateSelectedAccount: boolean;
    selectedAccountRoleBlocked: boolean;
    selectedAccountWarningMessage: string;
    saving: boolean;
    onSubmit: (event: FormEvent<HTMLFormElement>) => void;
    onClose: () => void;
    onOpenDetail: (account: Account) => void;
    onOpenEdit: (account: Account) => void;
    onOpenDelete: (account: Account) => void;
    onOpenResetPassword: (account: Account) => void;
    onDelete: () => void;
    onImpersonate: () => void;
    onResetPassword: () => void;
};

export function AccountModals(props: AccountModalsProps) {
    const {
        modalMode,
        selectedAccount,
        form,
        setForm,
        schoolOptions,
        manageableAccountTypes,
        canManageSelectedAccount,
        canImpersonateSelectedAccount,
        selectedAccountRoleBlocked,
        selectedAccountWarningMessage,
        saving,
        onSubmit,
        onClose,
        onOpenDetail,
        onOpenEdit,
        onOpenDelete,
        onOpenResetPassword,
        onDelete,
        onImpersonate,
        onResetPassword,
    } = props;

    return <>
        <Modal open={modalMode === "detail" || modalMode === "edit" || modalMode === "create"} onClose={onClose} className={style.userModal}>
            {modalMode === "edit" || modalMode === "create" ? (
                <AccountFormModal
                    mode={modalMode}
                    form={form}
                    setForm={setForm}
                    schoolOptions={schoolOptions}
                    manageableAccountTypes={manageableAccountTypes}
                    saving={saving}
                    onSubmit={onSubmit}
                    onCancel={() => selectedAccount ? onOpenDetail(selectedAccount) : onClose()}
                    previewAccount={selectedAccount}
                />
            ) : selectedAccount ? (
                <AccountDetailModal
                    account={selectedAccount}
                    canManage={canManageSelectedAccount}
                    canImpersonate={canImpersonateSelectedAccount}
                    showRoleWarning={selectedAccountRoleBlocked}
                    warningMessage={selectedAccountWarningMessage}
                    onEdit={() => onOpenEdit(selectedAccount)}
                    onDelete={() => onOpenDelete(selectedAccount)}
                    onImpersonate={onImpersonate}
                    onResetPassword={() => onOpenResetPassword(selectedAccount)}
                />
            ) : null}
        </Modal>

        <ModalDestructive
            open={modalMode === "delete" && selectedAccount !== null}
            title="Smazat uživatele"
            description={selectedAccount ? `Opravdu chceš smazat účet ${selectedAccount.fullName}? Tahle akce nejde vrátit.` : ""}
            confirmText="Smazat"
            loading={saving}
            onClose={() => selectedAccount ? onOpenDetail(selectedAccount) : onClose()}
            onConfirm={onDelete}
        />

        <ModalInformative
            open={modalMode === "reset-password" && selectedAccount !== null}
            title="Resetovat heslo"
            description={selectedAccount ? `Vygeneruje se nové heslo pro účet ${selectedAccount.fullName}. Heslo platí, dokud si ho uživatel nezmění.` : ""}
            confirmText="Resetovat"
            loading={saving}
            onClose={() => selectedAccount ? onOpenDetail(selectedAccount) : onClose()}
            onConfirm={onResetPassword}
        />
    </>
}

function AccountDetailModal({account, canManage, canImpersonate, showRoleWarning, warningMessage, onEdit, onDelete, onImpersonate, onResetPassword}: {
    account: Account,
    canManage: boolean,
    canImpersonate: boolean,
    showRoleWarning: boolean,
    warningMessage: string,
    onEdit: () => void,
    onDelete: () => void,
    onImpersonate: () => void,
    onResetPassword: () => void,
}) {
    return <>
        <div className={style.modalTop}>
            <div className={account.bannerUrl ? style.userdefinedBanner : style.generatedBanner} style={{
                backgroundImage: account.bannerUrl ? `url(${account.bannerUrl})` : account.avatarUrl ? `url(${account.avatarUrl})` : undefined,
            }}></div>
            <Avatar name={account.fullName} src={account.avatarUrl} size="200px" className={style.modalAvatar} />
        </div>
        <div className={style.modalBottom}>
            <h2>{account.fullName}</h2>
            <p className={account.enableReservations ? style.enabled : style.disabled}>{account.enableReservations ? "Rezervace povolené" : "Rezervace zakázané"}</p>

            <div className={style.profileActions}>
                {canManage && <button type="button" onClick={onEdit}>
                    <span style={{maskImage: "url(/icons/edit.svg)"}}></span>
                    Upravit
                </button>}
                <Link href={`/app/profile/${account.id}`}>
                    <span style={{maskImage: "url(/icons/account.svg)"}}></span>
                    Veřejný profil
                </Link>
            </div>

            {showRoleWarning && <div className={style.roleWarning}>
                <span style={{maskImage: "url(/icons/warn.svg)"}}></span>
                <p>{warningMessage}</p>
            </div>}

            <div className={style.infoRows}>
                <InfoRow icon="/icons/email.svg">{account.email}</InfoRow>
                <InfoRow icon="/icons/class.svg">{account.class || "-"}</InfoRow>
                <InfoRow icon="/icons/gender.svg">{genderLabel(account.gender)}</InfoRow>
                <InfoRow icon="/icons/user_with_shield.svg">{accountTypeLabel(account.accountType, account.gender)}</InfoRow>
                <InfoRow icon="/icons/organization.svg" title={account.school?.displayName}>{schoolLabel(account.school) || "-" }</InfoRow>
            </div>

            {canManage && <div className={style.modalActions}>
                <button type="button" className={style.dangerTextButton} onClick={onResetPassword}>
                    <span style={{maskImage: "url(/icons/reset_password.svg)"}}></span>
                    Resetovat heslo
                </button>
                {canImpersonate && <button type="button" className={style.impersonateButton} onClick={onImpersonate}>
                    <span style={{maskImage: "url(/icons/login.svg)"}}></span>
                    Přihlásit se za
                </button>}
                <button type="button" className={style.dangerButton} onClick={onDelete}>Smazat</button>
            </div>}
        </div>
    </>
}

function AccountFormModal({mode, form, setForm, schoolOptions, manageableAccountTypes, saving, onSubmit, onCancel, previewAccount}: {
    mode: "edit" | "create",
    form: AccountForm,
    setForm: Dispatch<SetStateAction<AccountForm>>,
    schoolOptions: SchoolOption[],
    manageableAccountTypes: AccountType[],
    saving: boolean,
    onSubmit: (event: FormEvent<HTMLFormElement>) => void,
    onCancel: () => void,
    previewAccount: Account | null,
}) {
    const update = <K extends keyof AccountForm>(key: K, value: AccountForm[K]) => setForm(current => ({...current, [key]: value}));
    const previewName = form.displayName.trim() || "?";

    return <form onSubmit={onSubmit}>
        <div className={style.modalTop}>
            <div className={form.bannerUrl ? style.userdefinedBanner : style.generatedBanner} style={{
                backgroundImage: form.bannerUrl ? `url(${form.bannerUrl})` : form.avatarUrl ? `url(${form.avatarUrl})` : previewAccount?.bannerUrl ? `url(${previewAccount.bannerUrl})` : undefined,
            }}></div>
            <Avatar name={previewName} src={form.avatarUrl || previewAccount?.avatarUrl} size="200px" className={style.modalAvatar} />
        </div>
        <div className={style.modalBottom}>
            <input className={style.nameInput} value={form.displayName} onChange={event => update("displayName", event.target.value)} placeholder={mode === "create" ? "Jméno" : "Jméno uživatele"} />

            <div className={style.formButtons}>
                <button type="submit" className={style.saveButton} disabled={saving}>
                    <span style={{maskImage: "url(/icons/successmark.svg)"}}></span>
                    {saving ? "Ukládám..." : "Uložit změny"}
                </button>
                <button type="button" className={style.cancelButton} onClick={onCancel}>
                    <span style={{maskImage: "url(/icons/cancel.svg)"}}></span>
                    Zrušit změny
                </button>
            </div>

            <div className={style.editRows}>
                <EditRow icon="/icons/email.svg">
                    <input type="email" value={form.email} onChange={event => update("email", event.target.value)} placeholder="Email" />
                </EditRow>
                <EditRow icon="/icons/class.svg">
                    <input value={form.class} onChange={event => update("class", event.target.value)} placeholder="Třída" />
                </EditRow>
                <EditRow icon="/icons/gender.svg">
                    <select value={form.gender} onChange={event => update("gender", event.target.value as AccountGender | "")}>
                        <option value="">Nezadáno</option>
                        <option value="Female">Žena</option>
                        <option value="Male">Muž</option>
                        <option value="Other">Ostatní</option>
                    </select>
                </EditRow>
                <EditRow icon="/icons/user_with_shield.svg">
                    <select value={form.accountType} onChange={event => update("accountType", event.target.value as AccountType)}>
                        {manageableAccountTypes.map(type => <option key={type} value={type}>{accountTypeFilterLabel(type)}</option>)}
                    </select>
                </EditRow>
                <EditRow icon="/icons/organization.svg">
                    <select value={form.schoolId} onChange={event => update("schoolId", event.target.value)}>
                        <option value="">Nezadáno</option>
                        {schoolOptions.map(school => <option key={school.id} value={school.id}>{school.label.length > 28 ? school.shortName : school.label}</option>)}
                    </select>
                </EditRow>
                <EditRow icon="/icons/account.svg">
                    <input value={form.avatarUrl} onChange={event => update("avatarUrl", event.target.value)} placeholder="Avatar URL" />
                </EditRow>
                <EditRow icon="/icons/map.svg">
                    <input value={form.bannerUrl} onChange={event => update("bannerUrl", event.target.value)} placeholder="Banner URL" />
                </EditRow>
                <EditRow icon="/icons/reset_password.svg">
                    <input type="text" value={form.password} onChange={event => update("password", event.target.value)} placeholder={mode === "create" ? "Heslo, prázdné = generovat" : "Nové heslo"} />
                </EditRow>
            </div>

            <div className={style.separator}></div>

            <label className={style.toggleRow}>
                <span>Povolit rezervace</span>
                <input type="checkbox" checked={form.enableReservations} onChange={event => update("enableReservations", event.target.checked)} />
            </label>

            <label className={style.toggleRow}>
                <span>Odeslat přihlašovací údaje na email</span>
                <input type="checkbox" checked={form.sendLoginCredentialsEmail} onChange={event => update("sendLoginCredentialsEmail", event.target.checked)} />
            </label>
        </div>
    </form>
}

function InfoRow({icon, children, title}: {icon: string, children?: ReactNode, title?: string}) {
    return <div className={style.infoRow}>
        <span className={style.rowIcon} style={{maskImage: `url(${icon})`}}></span>
        <p title={title}>{children}</p>
    </div>
}

function EditRow({icon, children}: {icon: string, children: ReactNode}) {
    return <label className={style.editRow}>
        <span className={style.rowIcon} style={{maskImage: `url(${icon})`}}></span>
        {children}
    </label>
}
