import type { Metadata } from 'next';
import {ReactNode} from "react";
import LayoutClient from "@/app/app/layoutclient";
import {siteConfig} from "@/data/site";

export const metadata: Metadata = {
    title: {
        default: "Systém » " + siteConfig.currentEvent.title,
        template: `%s » Systém ${siteConfig.currentEvent.title}`,
    },
    description: 'Systém pro správu a organizaci akce, včetně registrace účastníků, správy programů a dalších funkcí.',
}

export default function({ children }: { children: ReactNode }) {
    return <>
        <LayoutClient>
            {children}
        </LayoutClient>
    </>
}