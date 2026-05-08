import {siteConfig} from "@/data/site";
import type { Metadata } from 'next';
import {getCachedCurrentLoggedAccount, getCurrentLoggedAccount} from "@/lib/auth";
import {AuthProvider} from "@/app/app/_providers/AuthProvider";

export const metadata: Metadata = {
    title: {
        default: "Systém » " + siteConfig.brandName,
        template: `%s » Systém ${siteConfig.brandName}`,
    },
}


export default async function({ children }: {children: React.ReactNode}) {
    const loggedAccount = await getCachedCurrentLoggedAccount();

    return <AuthProvider initialUser={loggedAccount}>
        {children}
    </AuthProvider>
}