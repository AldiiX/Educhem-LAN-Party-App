"use client"

import style from "./client.module.scss"
import {Avatar} from "@/components/Avatar";
import {Account} from "@/schemas/AccountSchema";
import {translateGender} from "@/lib/translateEnums";

export default function({ account }: { account: Account }) {
    return <>
        <div className={style.banner} style={{ backgroundImage: `url(${account.bannerUrl})` }}>
            <Avatar name={account.fullName} src={account.avatarUrl} size="248px" className={style.avatar}/>
        </div>

        <div className={style.flex}>
            <div className={style.left}>
                <h1>{ account.fullName }</h1>
                <p>{ account.class }&nbsp;&nbsp;•&nbsp;&nbsp;{ translateGender(account.gender) }</p>
            </div>

            <div className={style.right}>
                <div className={style.school}>
                    <div className={style.img} style={{ backgroundImage: `url(${account.school.iconUrl})` }}></div>
                    <p>{ account.school.displayName }</p>
                </div>
            </div>
        </div>
    </>
}