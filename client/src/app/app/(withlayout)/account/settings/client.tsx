"use client";

import styles from "./client.module.scss";
import {Avatar} from "@/components/Avatar";
import {Button} from "@/components/Button";
import {communicationStyleLabel, genderLabel} from "@/lib/enumLabels";
import {AvatarSyncPlatform} from "@/schemas/AccountSchema";
import {AccountPageState} from "../_hooks/types";
import {avatarSyncPlatforms, ConnectablePlatform, platforms} from "@/data/platforms";
import {useAccountPageContext} from "@/app/app/(withlayout)/account/layoutclient";
import {ModalDestructive} from "@/components/ModalDialog";
import {phrase} from "@/lib/communicationStyle";
import {useState} from "react";
import {EmailChangeSettings} from "./_components/EmailChangeSettings";

const genderOptions = [
    {value: "Male", label: "Muž", disabled: false},
    {value: "Female", label: "Žena" , disabled: false},
    {value: "Other", label: "Ostatní", disabled: false},

    {value: "Other", label: "Tank", disabled: true},
    {value: "Other", label: "Helikoptéra", disabled: true},
    {value: "Other", label: "Sanitka Mercedes", disabled: true},
    {value: "Other", label: "Tatarka", disabled: true},
    {value: "Other", label: "Drak", disabled: true},
    {value: "Other", label: "Čajová konvice", disabled: true},
    {value: "Other", label: "Kabel RJ45", disabled: true},
    {value: "Other", label: "Labubu", disabled: true},
    {value: "Other", label: "Kobliha", disabled: true},
    {value: "Other", label: "Vesmírný pirát", disabled: true},
    {value: "Other", label: "Černá díra", disabled: true},
    {value: "Other", label: "Anténa 5G", disabled: true},
    {value: "Other", label: "Duhový paprsek", disabled: true},
    {value: "Other", label: "Houba shiitake", disabled: true},
    {value: "Other", label: "Štrúdl", disabled: true},
    {value: "Other", label: "Kávovar", disabled: true},
    {value: "Other", label: "VHS Kazeta", disabled: true},
    {value: "Other", label: "Pouliční lampa", disabled: true},
    {value: "Other", label: "Solární panel", disabled: true},
    {value: "Other", label: "Mechový koberec", disabled: true},
    {value: "Other", label: "Víkendový batoh", disabled: true},
    {value: "Other", label: "Origami jeřáb", disabled: true},
    {value: "Other", label: "Kapka rosy", disabled: true},
    {value: "Other", label: "Vzducholoď", disabled: true},
    {value: "Other", label: "Schrödingerova kočka", disabled: true},
    {value: "Other", label: "USB-C oboustranné", disabled: true},
    {value: "Other", label: "Toaletní voda", disabled: true},

    {value: "", label: "Neurčeno", disabled: false},
];

export default function AccountSettings() {
    const state = useAccountPageContext();
    const {account, profileDraft, setProfileDraft, passwordForm, setPasswordForm} = state;
    const [disconnectingPlatform, setDisconnectingPlatform] = useState<ConnectablePlatform | null>(null);
	const connectedPlatforms: Record<string, string | undefined> = {
		discord: account.discordUsername ?? undefined,
		github: account.githubUsername ?? undefined,
		google: account.googleName ?? undefined,
		apple: account.appleName ?? undefined,
		steam: account.steamUsername ?? undefined,
	};

    return <section className={styles.settings}>
        <div className={styles.connections}>
            <h2>Propojení</h2>
            <div className={styles.platforms}>
                {platforms.map(platform => (
                    <button key={platform.id} type="button" className={`${platform.disabled ? styles.disabled : ""} ${connectedPlatforms[platform.id] ? styles.connected : ""}`} disabled={platform.disabled || state.platformLoading} onClick={() => {
                        if(platform.disabled) return;
                        if(connectedPlatforms[platform.id]) {
                            setDisconnectingPlatform(platform.id);
                        } else {
                            state.connectPlatform(platform.id);
                        }
                    }}>
                        <span className={styles.platformIcon} style={{maskImage: `url(${platform.icon})`}}></span>
                        <span className={styles.platformName}>
                            <span>{platform.name}</span>
                            {connectedPlatforms[platform.id] && <small>{connectedPlatforms[platform.id]}</small>}
                        </span>
                        <span className={styles.platformAction}>{connectedPlatforms[platform.id] ? "Odpojit" : "+"}</span>
                    </button>
                ))}
            </div>
        </div>

        <ModalDestructive
            open={disconnectingPlatform !== null}
            title={`Odpojit ${platforms.find(platform => platform.id === disconnectingPlatform)?.name ?? "platformu"}`}
            description={phrase(
                account.communicationStyle,
                "Opravdu chceš odpojit tuto platformu od svého účtu?",
                "Opravdu chcete odpojit tuto platformu od svého účtu?",
            )}
            confirmText="Odpojit"
            cancelText="Zrušit"
            loading={state.platformLoading}
            onClose={() => setDisconnectingPlatform(null)}
            onConfirm={async () => {
                if(disconnectingPlatform === null) return;
                await state.disconnectPlatform(disconnectingPlatform);
                setDisconnectingPlatform(null);
            }}
        />

        <form className={styles.password} onSubmit={event => {
            event.preventDefault();
            state.changePassword();
        }}>
            <h2>Změna hesla</h2>
            <label>
                <span>Staré heslo</span>
                <input type="password" autoComplete="current-password" value={passwordForm.oldPassword} onChange={event => setPasswordForm({...passwordForm, oldPassword: event.target.value})} />
            </label>
            <label>
                <span>Nové heslo</span>
                <input type="password" autoComplete="new-password" value={passwordForm.newPassword} onChange={event => setPasswordForm({...passwordForm, newPassword: event.target.value})} />
            </label>
            <label>
                <span>Nové heslo potvrzení</span>
                <input type="password" autoComplete="new-password" value={passwordForm.newPasswordConfirmation} onChange={event => setPasswordForm({...passwordForm, newPasswordConfirmation: event.target.value})} />
            </label>

            {passwordForm.newPassword.length > 0 && <div className={styles.passwordRules}>
                <p className={state.passwordValidations.minLength ? styles.valid : ""}>Alespoň 8 znaků</p>
                <p className={state.passwordValidations.lower ? styles.valid : ""}>Alespoň 1 malé písmeno</p>
                <p className={state.passwordValidations.upper ? styles.valid : ""}>Alespoň 1 velké písmeno</p>
                <p className={state.passwordValidations.number ? styles.valid : ""}>Alespoň 1 číslo</p>
                <p className={state.passwordValidations.special ? styles.valid : ""}>Alespoň 1 speciální znak</p>
                <p className={state.passwordValidations.differentFromOld ? styles.valid : ""}>Nesmí být stejné jako staré heslo</p>
            </div>}

            <Button type="primary" text="Uložit změny" buttonType="submit" disabled={!state.canSubmitPassword || state.changingPassword} loading={state.changingPassword} />
        </form>

        <form className={styles.profile} onSubmit={event => {
            event.preventDefault();
            state.saveProfile();
        }}>
            <div className={styles.fields}>
                <h2>Editace profilu</h2>
                <label>
                    <span>Jméno</span>
                    <input type="text" value={account.fullName} disabled />
                </label>
                <EmailChangeSettings account={account} />
                <label>
                    <span>Třída</span>
                    <input type="text" value={account.enrollment?.class ?? "Žádná"} disabled />
                </label>
                <label>
                    <span>Pohlaví</span>
                    <select value={profileDraft.gender} onChange={event => setProfileDraft({...profileDraft, gender: event.target.value as typeof profileDraft.gender})}>
                        {genderOptions.map(option => <option key={option.label} value={option.value} disabled={option.disabled}>{option.label}</option>)}
                    </select>
                </label>
                <p className={styles.currentGender}>Aktuálně: {genderLabel(account.gender)}</p>
                <label>
                    <span>Oslovování</span>
                    <select value={profileDraft.communicationStyle} onChange={event => setProfileDraft({...profileDraft, communicationStyle: event.target.value as typeof profileDraft.communicationStyle})}>
                        <option value="Informal">Tykání</option>
                        <option value="Formal">Vykání</option>
                    </select>
                </label>
                <p className={styles.currentGender}>Aktuálně: {communicationStyleLabel(account.communicationStyle)}</p>

                <div className={styles.formActions}>
                    <Button type="secondary" text="Zrušit změny" onClick={state.resetProfileDraft} />
                    <Button type="primary" text="Uložit změny" buttonType="submit" loading={state.savingProfile} disabled={state.savingProfile} />
                </div>
            </div>

            <div className={styles.media}>
                <div className={styles.avatarEdit}>
                    <Avatar name={account.fullName} src={profileDraft.avatarUrl} size="168px" className={styles.avatarPreview} />
                    <div className={styles.avatarControls}>
                        <MediaButtons disabled={account.avatarSyncPlatform != null} onEdit={() => state.setModal("avatar-info")} onDelete={() => state.setModal("remove-avatar")} />
                        <label className={styles.avatarSyncPlatform}>
                            <span>Synchronizace avataru</span>
                            <select value={account.avatarSyncPlatform ?? ""} disabled={state.platformLoading} onChange={event => state.setAvatarSyncPlatform(event.target.value ? event.target.value as AvatarSyncPlatform : null)}>
                                {avatarSyncPlatforms.map(platform => <option key={platform.value} value={platform.value}>{platform.label}</option>)}
                            </select>
                        </label>
                    </div>
                </div>

                <div className={styles.bannerEdit}>
                    <div className={`${styles.bannerPreview} ${!profileDraft.bannerUrl ? styles.empty : ""}`} style={profileDraft.bannerUrl ? {backgroundImage: `url(${profileDraft.bannerUrl})`} : undefined}></div>
                    <MediaButtons onEdit={() => state.setModal("banner-info")} onDelete={() => state.setModal("remove-banner")} />
                </div>
            </div>
        </form>
    </section>;
}

function MediaButtons({disabled = false, onEdit, onDelete}: {disabled?: boolean; onEdit: () => void; onDelete: () => void}) {
    return <div className={styles.mediaButtons}>
        <button type="button" aria-label="Upravit" title={disabled ? "Avatar se synchronizuje z vybraného zdroje" : "Upravit"} onClick={onEdit} disabled={disabled}>
            <span style={{maskImage: "url(/icons/edit.svg)"}}></span>
        </button>
        <button type="button" aria-label="Smazat" title={disabled ? "Avatar se synchronizuje z vybraného zdroje" : "Smazat"} onClick={onDelete} disabled={disabled}>
            <span style={{maskImage: "url(/icons/trash.svg)"}}></span>
        </button>
    </div>;
}
