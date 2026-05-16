import type { Metadata } from 'next';
import {ReactNode} from "react";
import LayoutClient from "@/app/app/(withlayout)/layoutclient";
import {siteConfig} from "@/data/site";
import { version as appVersion } from "@/../package.json";

export const metadata: Metadata = {
    title: {
        default: "Systém » " + siteConfig.brandName,
        template: `%s » Systém ${siteConfig.brandName}`,
    },
    description: 'Systém pro správu a organizaci akce, včetně registrace účastníků, správy programů a dalších funkcí.',
}

export default async function({ children }: { children: ReactNode }) {
    return <>
        <LayoutClient appVersion={appVersion}>
            {children}
        </LayoutClient>
    </>
}