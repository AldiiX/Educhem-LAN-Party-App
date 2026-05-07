import {siteConfig} from "@/data/site";
import type { Metadata } from 'next';

export const metadata: Metadata = {
    title: {
        default: "Systém » " + siteConfig.brandName,
        template: `%s » Systém ${siteConfig.brandName}`,
    },
}


export default function({ children }: {children: React.ReactNode}) {
    return children;
}