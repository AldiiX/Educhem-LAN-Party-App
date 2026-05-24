import type { Metadata } from 'next';
import {ReactNode} from "react";
import LayoutClient from "@/app/app/(withlayout)/layoutclient";
import {siteConfig} from "@/data/site";

export const metadata: Metadata = {
    title: {
        default: "Systém » " + siteConfig.brandName,
        template: `%s » Systém ${siteConfig.brandName}`,
    },
}

export default async function({ children }: { children: ReactNode }) {
    return <>
        <LayoutClient>
            {children}
        </LayoutClient>
    </>
}
