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
        return null;
    }

    return (
        <section className={style.appsettingsTab}>
            <div className={style.settingsCard}>
                <form
                    key={`reservations-${settings.appSettings.reservationsStatus}-${settings.appSettings.reservationsEnabledFrom.toISOString()}-${settings.appSettings.reservationsEnabledTo.toISOString()}`}
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
                            <If condition={settings.reservationsStatus === "UseTimer"}>
                                <ReservationTimerInfo
                                    serverNow={settings.appSettings.serverNow}
                                    from={settings.appSettings.reservationsEnabledFrom}
                                    to={settings.appSettings.reservationsEnabledTo}
                                    reservationsEnabledRightNow={settings.appSettings.reservationsEnabledRightNow}
                                    onFinished={() => settings.mutate()}
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
                <form
                    key={`features-${String(settings.appSettings.chatEnabled)}-${String(settings.appSettings.attendanceEnabled)}-${String(settings.appSettings.problemReportsEnabled)}`}
                    onSubmit={settings.submitFeatureSettings}
                >
                    <div className={style.cardHeader}>
                        <h2>Nastavení funkcí</h2>
                        <p>Zapnutí nebo uzamčení dostupných funkcí.</p>
                    </div>

                    <label className={style.field}>
                        <span>Chat</span>
                        <select name="chatEnabled" defaultValue={String(settings.appSettings.chatEnabled)}>
                            <option value="true">Povolené</option>
                            <option value="false">Zakázané</option>
                        </select>
                    </label>

                    <label className={style.field}>
                        <span>Docházka</span>
                        <select name="attendanceEnabled" defaultValue={String(settings.appSettings.attendanceEnabled)}>
                            <option value="true">Povolená</option>
                            <option value="false">Uzamčená</option>
                        </select>
                    </label>

                    <label className={style.field}>
                        <span>Hlášení problémů</span>
                        <select name="problemReportsEnabled" defaultValue={String(settings.appSettings.problemReportsEnabled)}>
                            <option value="true">Povolené</option>
                            <option value="false">Uzamčené</option>
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

            <div className={style.settingsCard}>
                <div className={style.cachePanel}>
                    <div className={style.cardHeader}>
                        <h2>Cache aplikace</h2>
                        <p>Vyprázdnění všech dat uložených v aplikační cache.</p>
                    </div>

                    <button
                        type="button"
                        className={style.dangerButton}
                        disabled={settings.clearingCache}
                        onClick={settings.clearAppCache}
                    >
                        {settings.clearingCache ? "Mažu cache..." : "Smazat cache"}
                    </button>

                    <If condition={settings.lastCacheClear !== null}>
                        <div className={style.cacheStats}>
                            <span>Odstraněné klíče: {settings.lastCacheClear?.removedKeys ?? 0}</span>
                            <span>
                                Sledované klíče: {settings.lastCacheClear?.trackedKeysBefore ?? 0}
                                {" -> "}
                                {settings.lastCacheClear?.trackedKeysAfter ?? 0}
                            </span>
                            <span>
                                Managed paměť .NET: {formatBytes(settings.lastCacheClear?.managedMemoryBeforeBytes ?? 0)}
                                {" -> "}
                                {formatBytes(settings.lastCacheClear?.managedMemoryAfterBytes ?? 0)}
                            </span>
                        </div>
                    </If>
                </div>
            </div>
        </section>
    );
}

function ReservationTimerInfo({
    serverNow,
        from,
        to,
        reservationsEnabledRightNow,
        onFinished,
}: {
    serverNow: Date;
    from: Date;
    to: Date;
    reservationsEnabledRightNow: boolean;
    onFinished: () => void;
}) {
    const serverNowMs = serverNow.getTime();
    const fromMs = from.getTime();
    const toMs = to.getTime();

    if (!Number.isFinite(serverNowMs) || !Number.isFinite(fromMs) || !Number.isFinite(toMs)) {
        return (
            <div className={style.timerInfo}>
                Časovač se nepodařilo načíst.
            </div>
        );
    }

    if (serverNowMs < fromMs) {
        return (
            <div className={style.timerInfo}>
                <CountdownTimer
                    serverNow={serverNow}
                    target={from}
                    prefix="Rezervace se povolí za"
                    finishedText="Aktualizuji stav rezervací..."
                    onFinished={onFinished}
                    className={style.countdown}
                />
            </div>
        );
    }

    if (reservationsEnabledRightNow && serverNowMs <= toMs) {
        return (
            <div className={`${style.timerInfo} ${style.timerInfoEnabled}`}>
                <p>Rezervace jsou právě povolené.</p>

                <CountdownTimer
                    serverNow={serverNow}
                    target={to}
                    prefix="Zavřou se za"
                    finishedText="Aktualizuji stav rezervací..."
                    onFinished={onFinished}
                    className={style.countdown}
                />
            </div>
        );
    }

    if (serverNowMs > toMs) {
        return (
            <div className={`${style.timerInfo} ${style.timerInfoDisabled}`}>
                Časovač už skončil. Rezervace jsou zavřené.
            </div>
        );
    }

    return (
        <div className={`${style.timerInfo} ${style.timerInfoDisabled}`}>
            Rezervace jsou podle časovače zavřené.
        </div>
    );
}

function toDatetimeLocal(value: Date) {
    const pad = (n: number) => n.toString().padStart(2, "0");

    return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`;
}

function formatBytes(bytes: number) {
    if (!Number.isFinite(bytes) || bytes <= 0) {
        return "0 B";
    }

    const units = ["B", "KB", "MB", "GB"];
    let value = bytes;
    let unit = 0;

    while (value >= 1024 && unit < units.length - 1) {
        value /= 1024;
        unit += 1;
    }

    return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
}
