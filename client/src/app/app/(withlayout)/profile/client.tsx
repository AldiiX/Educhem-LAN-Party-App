"use client"

import {useEffect, useState} from "react";
import style from "./client.module.scss"
import {Avatar} from "@/components/Avatar";
import {Account, AccountSchema} from "@/schemas/AccountSchema";
import {translateGender} from "@/lib/translateEnums";
import If from "@/components/util/If";
import {accountTypeLabel, genderLabel} from "@/lib/enumLabels";

export default function({ account }: { account: Account }) {
    const [profile, setProfile] = useState(account);

    useEffect(() => {
        let active = true;

        async function refreshProfile() {
            try {
                const response = await fetch(`/api/v1/profile/${account.id}`, {cache: "no-cache"});
                if(!response.ok) return;
                const json = await response.json();
                const parsed = AccountSchema.safeParse(json);
                if(active && parsed.success) setProfile(parsed.data);
            } catch {
                // Ignore refresh failures
            }
        }

        const source = new EventSource(`/api/v1/profile/${account.id}/events`);
        const onUpdate = () => refreshProfile();
        source.addEventListener("update", onUpdate);

        refreshProfile();

        return () => {
            active = false;
            source.removeEventListener("update", onUpdate);
            source.close();
        };
    }, [account.id]);

    const achievements = (profile.achievements ?? []).filter((entry) => !entry.isHidden);
    const badges = (profile.badges ?? []).filter((entry) => entry.isTakenOut);

    return <>
        <div className={style.banner} style={{ '--banner': `url(${account.bannerUrl})` } as React.CSSProperties}>
            <Avatar name={account.fullName} src={account.avatarUrl} size="248px" className={style.avatar} />
        </div>

        <div className={style.flex}>
            <div className={style.left}>
                <h1>{ profile.fullName }</h1>
                {/*<p>{ els.join("   •   ") }</p>*/}
                <p>{ accountTypeLabel(profile.accountType, profile.gender) }</p>

                <div className={style.items}>
                    <If condition={profile.class != null} as="div" className={style.item} title={`Třída: ${profile.class}`}>
                        <div className={style.icon} style={{ maskImage: `url(/icons/class.svg)` }}></div>
                        <p>{ profile.class }</p>
                    </If>

                    <If condition={profile.gender != null} as="div" className={style.item} title={`Pohlaví: ${translateGender(profile.gender)}`}>
                        <div className={style.icon} style={{ maskImage: `url(/icons/gender.svg)` }}></div>
                        <p>{ genderLabel(profile.gender) }</p>
                    </If>

                    <If condition={profile.createdAtUtc != null} as="div" className={style.item} title={`Datum registrace: ${profile.createdAtUtc.toLocaleDateString()}`}>
                        <div className={style.icon} style={{ maskImage: `url(/icons/login.svg)` }}></div>
                        <p>{ profile.createdAtUtc.toLocaleDateString() }</p>
                    </If>
                </div>
            </div>

            <div className={style.right}>
                <If condition={profile.school != null} as="div" className={style.school}>
                    <div className={style.img} style={{ backgroundImage: `url(${profile.school?.iconUrl})` }}></div>
                    <p>{ profile.school?.displayName }</p>
                </If>

                <div className={style.achievementBar}>
                    <div className={style.barHeader}>
                        <h3>Achievementy</h3>
                        <span>{achievements.length}</span>
                    </div>
                    {achievements.length === 0 ? (
                        <p className={style.barEmpty}>Zatím žádné achievementy.</p>
                    ) : (
                        <div className={style.achievementList}>
                            {achievements.map((entry) => (
                                <div key={entry.id} className={style.achievementRow}>
                                    <div
                                        className={style.achievementCover}
                                        style={entry.achievement.iconUrl ? { backgroundImage: `url(${entry.achievement.iconUrl})` } : undefined}
                                    >
                                        {!entry.achievement.iconUrl && <span>{entry.achievement.name.charAt(0)}</span>}
                                    </div>
                                    <div className={style.achievementText}>
                                        <p className={style.achievementTitle}>{entry.achievement.name}</p>
                                        {entry.achievement.description && <p className={style.achievementDescription}>{entry.achievement.description}</p>}
                                        <p className={style.achievementDate}>Získáno: {entry.createdAtUtc.toLocaleDateString("cs-CZ")}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className={style.badgeBar}>
                    <div className={style.barHeader}>
                        <h3>Badge</h3>
                        <span>{badges.length}</span>
                    </div>
                    {badges.length === 0 ? (
                        <p className={style.barEmpty}>Zatím žádné badge.</p>
                    ) : (
                        <div className={style.barItems}>
                            {badges.map((entry) => (
                                <div key={entry.id} className={style.barItem} title={entry.badge.name}>
                                    {entry.badge.iconUrl ? (
                                        <img src={entry.badge.iconUrl} alt="" />
                                    ) : (
                                        <span>{entry.badge.name.charAt(0)}</span>
                                    )}
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    </>
}