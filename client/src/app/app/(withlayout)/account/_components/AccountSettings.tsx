import styles from "./AccountSettings.module.scss";
import {Avatar} from "@/components/Avatar";
import {Button} from "@/components/Button";
import {genderLabel} from "@/lib/enumLabels";
import {AccountPageState} from "../_hooks/types";


const platforms = [
    {id: "discord", name: "Discord", icon: "/icons/discord.svg", disabled: true},
    {id: "github", name: "GitHub", icon: "/icons/github.svg", disabled: true},
    {id: "google", name: "Google", icon: "/icons/google.svg", disabled: true},
    {id: "instagram", name: "Instagram", icon: "/icons/instagram.svg", disabled: true},
];

const genderOptions = [
    {value: "Male", label: "Muž"},
    {value: "Female", label: "Žena"},
    {value: "Other", label: "Ostatní"},
    {value: "", label: "Neurčeno"},
];

export function AccountSettings({state}: {state: AccountPageState}) {
    const {account, profileDraft, setProfileDraft, passwordForm, setPasswordForm} = state;

    return <section className={styles.settings}>
        <div className={styles.connections}>
            <h2>Propojení</h2>
            <div className={styles.platforms}>
                {platforms.map(platform => (
                    <button key={platform.id} type="button" className={platform.disabled ? styles.disabled : ""} disabled={platform.disabled}>
                        <span className={styles.platformIcon} style={{maskImage: `url(${platform.icon})`}}></span>
                        <span>{platform.name}</span>
                        <span className={styles.platformAction}>+</span>
                    </button>
                ))}
            </div>
        </div>

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
                <label>
                    <span>Email</span>
                    <input type="text" value={account.email ?? ""} disabled />
                </label>
                <label>
                    <span>Třída</span>
                    <input type="text" value={account.class ?? "Žádná"} disabled />
                </label>
                <label>
                    <span>Pohlaví</span>
                    <select value={profileDraft.gender} onChange={event => setProfileDraft({...profileDraft, gender: event.target.value as typeof profileDraft.gender})}>
                        {genderOptions.map(option => <option key={option.label} value={option.value}>{option.label}</option>)}
                    </select>
                </label>
                <p className={styles.currentGender}>Aktuálně: {genderLabel(account.gender)}</p>

                <div className={styles.formActions}>
                    <Button type="secondary" text="Zrušit změny" onClick={state.resetProfileDraft} />
                    <Button type="primary" text="Uložit změny" buttonType="submit" loading={state.savingProfile} disabled={state.savingProfile} />
                </div>
            </div>

            <div className={styles.media}>
                <div className={styles.avatarEdit}>
                    <Avatar name={account.fullName} src={profileDraft.avatarUrl} size="168px" className={styles.avatarPreview} />
                    <MediaButtons onEdit={() => state.setModal("avatar-info")} onDelete={() => state.setModal("remove-avatar")} />
                </div>

                <div className={styles.bannerEdit}>
                    <div className={`${styles.bannerPreview} ${!profileDraft.bannerUrl ? styles.empty : ""}`} style={profileDraft.bannerUrl ? {backgroundImage: `url(${profileDraft.bannerUrl})`} : undefined}></div>
                    <MediaButtons onEdit={() => state.setModal("banner-info")} onDelete={() => state.setModal("remove-banner")} />
                </div>
            </div>
        </form>
    </section>;
}

function MediaButtons({onEdit, onDelete}: {onEdit: () => void; onDelete: () => void}) {
    return <div className={styles.mediaButtons}>
        <button type="button" aria-label="Upravit" title="Upravit" onClick={onEdit}>
            <span style={{maskImage: "url(/icons/edit.svg)"}}></span>
        </button>
        <button type="button" aria-label="Smazat" title="Smazat" onClick={onDelete}>
            <span style={{maskImage: "url(/icons/trash.svg)"}}></span>
        </button>
    </div>;
}
