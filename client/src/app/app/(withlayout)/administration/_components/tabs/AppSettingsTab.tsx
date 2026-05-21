"use client";

import style from "./AppSettingsTab.module.scss";
import If from "../../../../../../components/util/If";
import {CountdownTimer} from "../CountdownTimer";
import {useAppSettings} from "../../_hooks/useAppSettings";
import {ReservationStatusType} from "@/schemas/AppSettingsSchema";

export function AppSettingsTab() {
    const settings = useAppSettings();

    if (settings.error) {
        return (
            <>
                <p>Nastavení aplikace se nepodařilo načíst.</p>
                <button type="button" onClick={() => settings.mutate()}>
                    Zkusit znovu
                </button>
            </>
        );
    }

    if (settings.isLoading || !settings.appSettings) {
        return <p>Načítám nastavení...</p>;
    }

    return (
        <section className={style.appsettingsTab}>
            <div className={style.settingsCard}>
                <form
                    onSubmit={settings.submitReservations}
                    onReset={() => settings.setReservationsStatus(settings.appSettings!.reservationsStatus)}
                >
                    <div className={style.cardHeader}>
                        <h2>Nastavení rezervací</h2>
                        <p>Řízení dostupnosti rezervací.</p>
                    </div>

                    <label className={style.field}>
                        <span>Status</span>
                        <select
                            name="reservationsStatus"
                            value={settings.reservationsStatus}
                            onChange={event =>
                                settings.setReservationsStatus(event.target.value as ReservationStatusType)
                            }
                        >
                            <option value="UseTimer">Použít časovač</option>
                            <option value="Open">Zapnuto</option>
                            <option value="Closed">Vypnuto</option>
                        </select>
                    </label>

                    <If condition={settings.reservationsStatus === "UseTimer"}>
                        <div className={style.timerInfo}>
                            <If
                                condition={!settings.appSettings.reservationsEnabledRightNow}
                                fallback={
                                    <CountdownTimer
                                        serverNow={settings.appSettings.serverNow}
                                        target={settings.appSettings.reservationsEnabledTo}
                                        prefix="Rezervace se zavřou za "
                                        finishedText="Rezervace jsou podle časovače zavřené."
                                    />
                                }
                            >
                                <CountdownTimer
                                    serverNow={settings.appSettings.serverNow}
                                    target={settings.appSettings.reservationsEnabledFrom}
                                    prefix="Rezervace se povolí za "
                                    finishedText="Rezervace jsou podle časovače povolené."
                                />
                            </If>
                        </div>

                        <label className={style.field}>
                            <span>Povoleno od</span>
                            <input
                                type="datetime-local"
                                name="reservationsEnabledFrom"
                                defaultValue={toDatetimeLocal(settings.appSettings.reservationsEnabledFrom)}
                            />
                        </label>

                        <label className={style.field}>
                            <span>Povoleno do</span>
                            <input
                                type="datetime-local"
                                name="reservationsEnabledTo"
                                defaultValue={toDatetimeLocal(settings.appSettings.reservationsEnabledTo)}
                            />
                        </label>
                    </If>

                    <div className={style.buttons}>
                        <button type="reset" className={style.cancelButton}>
                            Zrušit změny
                        </button>

                        <button type="submit" className={style.saveButton} disabled={settings.saving}>
                            {settings.saving ? "Ukládám..." : "Uložit změny"}
                        </button>
                    </div>
                </form>
            </div>

            <div className={style.settingsCard}>
                <form onSubmit={settings.submitChat}>
                    <div className={style.cardHeader}>
                        <h2>Nastavení chatu</h2>
                        <p>Zapnutí nebo vypnutí chatu.</p>
                    </div>

                    <label className={style.field}>
                        <span>Viditelnost</span>
                        <select name="chatEnabled" defaultValue={String(settings.appSettings.chatEnabled)}>
                            <option value="true">Povolené</option>
                            <option value="false">Zakázané</option>
                        </select>
                    </label>

                    <div className={style.buttons}>
                        <button type="reset" className={style.cancelButton}>
                            Zrušit změny
                        </button>

                        <button type="submit" className={style.saveButton} disabled={settings.saving}>
                            {settings.saving ? "Ukládám..." : "Uložit změny"}
                        </button>
                    </div>
                </form>
            </div>
        </section>
    );
}

function toDatetimeLocal(value: Date) {
    const pad = (n: number) => n.toString().padStart(2, "0");

    return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`;
}