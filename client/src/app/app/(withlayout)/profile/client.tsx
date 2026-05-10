"use client"

import style from "./client.module.scss"
import {Avatar} from "@/components/Avatar";
import {Account} from "@/schemas/AccountSchema";
import {translateGender} from "@/lib/translateEnums";
import If from "@/components/util/If";
import {accountTypeLabel, genderLabel} from "@/lib/enumLabels";

export default function({ account }: { account: Account }) {
    /*const els = [
        account.class,
        account.gender ? translateGender(account.gender) : null,
        accountTypeLabel(account.accountType, account.gender)
    ].filter(Boolean) as string[];*/

    return <>
        <div className={style.banner} style={{ backgroundImage: `url(${account.bannerUrl})` }}>
            <Avatar name={account.fullName} src={account.avatarUrl} size="248px" className={style.avatar}/>
        </div>

        <div className={style.flex}>
            <div className={style.left}>
                <h1>{ account.fullName }</h1>
                {/*<p>{ els.join("   •   ") }</p>*/}
                <p>{ accountTypeLabel(account.accountType, account.gender) }</p>

                <div className={style.items}>
                    <If condition={account.class != null} as="div" className={style.item} title={`Třída: ${account.class}`}>
                        <div className={style.icon} style={{ maskImage: `url(/icons/class.svg)` }}></div>
                        <p>{ account.class }</p>
                    </If>

                    <If condition={account.gender != null} as="div" className={style.item} title={`Pohlaví: ${translateGender(account.gender)}`}>
                        <div className={style.icon} style={{ maskImage: `url(/icons/gender.svg)` }}></div>
                        <p>{ genderLabel(account.gender) }</p>
                    </If>

                    <If condition={account.createdAtUtc != null} as="div" className={style.item} title={`Datum registrace: ${account.createdAtUtc.toLocaleDateString()}`}>
                        <div className={style.icon} style={{ maskImage: `url(/icons/plus.svg)` }}></div>
                        <p>{ account.createdAtUtc.toLocaleDateString() }</p>
                    </If>
                </div>
            </div>

            <div className={style.right}>
                <If condition={account.school != null} as="div" className={style.school}>
                    <div className={style.img} style={{ backgroundImage: `url(${account.school?.iconUrl})` }}></div>
                    <p>{ account.school?.displayName }</p>
                </If>
            </div>
        </div>
    </>
}