"use client";

import Link from "next/link";
import styles from "./client.module.scss";
import {Avatar} from "@/components/Avatar";
import {ProfileHoverCard} from "@/components/ProfileHoverCard";
import {
    AttendanceEntry,
    AttendanceParticipant,
} from "@/schemas/AttendanceSchema";
import {attendanceActionLabels, useAttendance} from "./_hooks/useAttendance";

export default function AttendanceClient() {
    const attendance = useAttendance();
    const isInitialLoading = attendance.isLoading && !attendance.data;

    if(attendance.error) {
        return <main className={styles.attendance}>
            <h1>Docházka</h1>
            <p className={styles.error}>Docházku se nepodařilo načíst.</p>
            <button type="button" className={styles.retryButton} onClick={() => attendance.mutate()}>Zkusit znovu</button>
        </main>;
    }

    if(isInitialLoading) {
        return <AttendanceSkeleton />;
    }

    return <main className={styles.attendance}>
        <header className={styles.header}>
            <div>
                <h1>Docházka</h1>
                <p>Aktuální přehled přítomnosti účastníků na akci.</p>
            </div>
        </header>

        <section className={styles.stats}>
            <StatCard icon="/icons/user_in_building.svg" label="Na akci" value={attendance.data?.stats.present ?? 0} />
            <StatCard icon="/icons/logout.svg" label="Pryč" value={attendance.data?.stats.away ?? 0} />
            <StatCard icon="/icons/statistics.svg" label="Účastníků" value={attendance.data?.stats.total ?? 0} />
        </section>

        <section className={styles.grid}>
            <form className={styles.form} onSubmit={attendance.submit}>
                <div className={styles.sectionHeader}>
                    <h2>{attendanceActionLabels[attendance.nextType]}</h2>
                    <span className={attendance.nextType === "CheckIn" ? styles.arrival : styles.departure}>{attendanceActionLabels[attendance.nextType]}</span>
                </div>

                {attendance.data?.attendanceEnabled === false && (
                    <p className={styles.error}>Docházka je momentálně uzamčená.</p>
                )}

                {attendance.canManageAttendance && (
                    <label>
                        <span>Účastník</span>
                        <select value={attendance.selectedAccountId} onChange={event => attendance.setSelectedAccountId(event.target.value)}>
                            <option value="">Já</option>
                            {attendance.data?.participants.map(participant => (
                                <option key={participant.profile.id} value={participant.profile.id}>
                                    {participant.profile.fullName}{participant.profile.class ? `, ${participant.profile.class}` : ""}
                                </option>
                            ))}
                        </select>
                    </label>
                )}

                {attendance.selectedParticipant && <ParticipantStatus participant={attendance.selectedParticipant} />}

                <label>
                    <span>Důvod</span>
                    <textarea
                        value={attendance.reason}
                        onChange={event => attendance.setReason(event.target.value)}
                        placeholder={attendance.nextType === "CheckOut" ? "Např. jdu si něco koupit" : "Volitelné"}
                        required={attendance.nextType === "CheckOut"}
                    />
                </label>

                {attendance.submitError && <p className={styles.error}>{attendance.submitError}</p>}

                <button type="submit" disabled={attendance.isSubmitting || attendance.isLoading || attendance.data?.attendanceEnabled === false}>
                    <span style={{maskImage: `url(${attendance.nextType === "CheckIn" ? "/icons/login.svg" : "/icons/logout.svg"})`}} />
                    {attendance.isSubmitting ? "Ukládám..." : `Zapsat ${attendance.nextType === "CheckIn" ? "příchod" : "odchod"}`}
                </button>
            </form>

            <section className={styles.participants}>
                <div className={styles.sectionHeader}>
                    <h2>Účastníci</h2>
                    <span>{attendance.data?.participants.length ?? 0}</span>
                </div>

                <div className={styles.participantList}>
                    {attendance.data?.participants.map(participant => (
                        <ParticipantRow key={participant.profile.id} participant={participant} />
                    ))}
                    {attendance.isLoading && <p className={styles.muted}>Načítám docházku...</p>}
                </div>
            </section>
        </section>

        <section className={styles.timeline}>
            <div className={styles.timelineHeader}>
                <div>
                    <h2>Záznamy</h2>
                    <p>{attendance.filteredEntries.length} záznamů</p>
                </div>
                <div className={styles.searchBox}>
                    <span style={{maskImage: "url(/icons/account.svg)"}} />
                    <input value={attendance.search} onChange={event => attendance.setSearch(event.target.value)} placeholder="Hledat..." />
                </div>
            </div>

            <div className={styles.entries}>
                {attendance.filteredEntries.map(entry => <AttendanceEntryRow key={entry.id} entry={entry} />)}
                {!attendance.isLoading && attendance.filteredEntries.length === 0 && <p className={styles.muted}>Zatím tu nejsou žádné záznamy.</p>}
            </div>
        </section>
    </main>;
}

function AttendanceSkeleton() {
    return <main className={styles.attendance} aria-busy="true">
        <header className={styles.header}>
            <div>
                <h1>Docházka</h1>
                <p>Aktuální přehled přítomnosti účastníků na akci.</p>
            </div>
        </header>

        <section className={styles.stats}>
            {Array.from({length: 3}).map((_, index) => <div key={index} className={`${styles.statCard} ${styles.skeletonStat}`}>
                <SkeletonBlock className={styles.skeletonIcon} />
                <div>
                    <SkeletonBlock className={styles.skeletonTextShort} />
                    <SkeletonBlock className={styles.skeletonNumber} />
                </div>
            </div>)}
        </section>

        <section className={styles.grid}>
            <div className={`${styles.form} ${styles.skeletonPanel}`}>
                <div className={styles.sectionHeader}>
                    <SkeletonBlock className={styles.skeletonHeading} />
                    <SkeletonBlock className={styles.skeletonPill} />
                </div>
                <SkeletonBlock className={styles.skeletonField} />
                <SkeletonBlock className={styles.skeletonStatus} />
                <SkeletonBlock className={styles.skeletonArea} />
                <SkeletonBlock className={styles.skeletonButton} />
            </div>

            <section className={`${styles.participants} ${styles.skeletonPanel}`}>
                <div className={styles.sectionHeader}>
                    <SkeletonBlock className={styles.skeletonHeading} />
                    <SkeletonBlock className={styles.skeletonPill} />
                </div>
                <div className={styles.participantList}>
                    {Array.from({length: 5}).map((_, index) => <SkeletonRow key={index} />)}
                </div>
            </section>
        </section>

        <section className={`${styles.timeline} ${styles.skeletonPanel}`}>
            <div className={styles.timelineHeader}>
                <div>
                    <SkeletonBlock className={styles.skeletonHeading} />
                    <SkeletonBlock className={styles.skeletonTextShort} />
                </div>
                <SkeletonBlock className={styles.skeletonSearch} />
            </div>

            <div className={styles.entries}>
                {Array.from({length: 4}).map((_, index) => <SkeletonEntry key={index} />)}
            </div>
        </section>
    </main>;
}

function SkeletonRow() {
    return <div className={styles.skeletonRow}>
        <SkeletonBlock className={styles.skeletonAvatar} />
        <div>
            <SkeletonBlock className={styles.skeletonText} />
            <SkeletonBlock className={styles.skeletonTextTiny} />
        </div>
        <SkeletonBlock className={styles.skeletonPill} />
    </div>;
}

function SkeletonEntry() {
    return <article className={`${styles.entry} ${styles.skeletonEntry}`}>
        <div className={styles.skeletonRow}>
            <SkeletonBlock className={styles.skeletonAvatar} />
            <div>
                <SkeletonBlock className={styles.skeletonText} />
                <SkeletonBlock className={styles.skeletonTextTiny} />
            </div>
        </div>
        <div className={styles.entryBody}>
            <SkeletonBlock className={styles.skeletonText} />
            <SkeletonBlock className={styles.skeletonTextLong} />
        </div>
    </article>;
}

function SkeletonBlock({className}: {className: string}) {
    return <span className={`${styles.skeletonBlock} ${className}`} />;
}

function StatCard({icon, label, value}: {icon: string; label: string; value: number}) {
    return <div className={styles.statCard}>
        <span style={{maskImage: `url(${icon})`}} />
        <div>
            <p>{label}</p>
            <strong>{value}</strong>
        </div>
    </div>;
}

function ParticipantStatus({participant}: {participant: AttendanceParticipant}) {
    const isPresent = participant.currentState === "CheckIn";

    return <div className={styles.currentStatus}>
        <Avatar name={participant.profile.fullName} src={participant.profile.avatarUrl} size="40px" />
        <div>
            <strong>{participant.profile.fullName}</strong>
            <p>{isPresent ? "Aktuálně na akci" : "Aktuálně pryč"}</p>
        </div>
    </div>;
}

function ParticipantRow({participant}: {participant: AttendanceParticipant}) {
    const isPresent = participant.currentState === "CheckIn";

    return <ProfileHoverCard account={participant.profile}>
        <Link href={`/app/profile/${participant.profile.id}`} className={styles.participant}>
            <Avatar name={participant.profile.fullName} src={participant.profile.avatarUrl} size="36px" />
            <span>
                <strong>{participant.profile.fullName}</strong>
                <small>{participant.profile.class ?? "Bez třídy"}</small>
            </span>
            <em className={isPresent ? styles.arrival : styles.departure}>{isPresent ? "Na akci" : "Pryč"}</em>
        </Link>
    </ProfileHoverCard>;
}

function AttendanceEntryRow({entry}: {entry: AttendanceEntry}) {
    const isArrival = entry.type === "CheckIn";

    return <article className={styles.entry}>
        <ProfileHoverCard account={entry.profile}>
            <Link href={`/app/profile/${entry.profile.id}`} className={styles.entryProfile}>
                <Avatar name={entry.profile.fullName} src={entry.profile.avatarUrl} size="42px" />
                <span>
                    <strong>{entry.profile.fullName}</strong>
                    <small>{entry.profile.class ?? "Bez třídy"}</small>
                </span>
            </Link>
        </ProfileHoverCard>

        <div className={styles.entryBody}>
            <div>
                <span className={isArrival ? styles.arrival : styles.departure}>{attendanceActionLabels[entry.type]}</span>
                <time>{entry.createdAtUtc.toLocaleString("cs-CZ")}</time>
            </div>
            {entry.reason && <p>{entry.reason}</p>}
            {entry.createdBy.id !== entry.profile.id && <small>Zapsal/a {entry.createdBy.fullName}</small>}
        </div>
    </article>;
}
