"use client"

import style from "./client.module.scss"
import {Avatar} from "@/components/Avatar";
import {Account} from "@/schemas/AccountSchema";
import {translateGender} from "@/lib/translateEnums";
import If from "@/components/util/If";
import {accountTypeLabel} from "@/lib/enumLabels";

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