import {siteConfig} from "@/data/site";
import type { Metadata } from 'next';
import {getCachedCurrentLoggedAccount, getCurrentLoggedAccount} from "@/lib/auth";
import {AuthProvider} from "@/app/app/_providers/AuthProvider";
import {Button} from "@/components/Button";
import Link from 'next/link'


export const metadata: Metadata = {
    title: {
        default: "Systém » " + siteConfig.brandName,
        template: `%s » Systém ${siteConfig.brandName}`,
    },
}

const SYSTEM_ENABLED: boolean = true;


export default async function({ children }: {children: React.ReactNode}) {
    if(!SYSTEM_ENABLED) { // TODO: přesunout do jine stranky, na kterou bude redirektovat -> takto vznikaji bugy
        return (
            <div style={{ position: "absolute", top: "50%", left: "50%", transform: "translate(-50%, -50%)" }}>
                <h1>Systém je aktuálně vypnutý...</h1>
                <Link href={"/"}>
                    <Button type={"secondary"} text="Zpět na hlavní stránku" style={{ margin: "12px auto"}}  />
                </Link>
            </div>
        );
    }

    const loggedAccount = await getCachedCurrentLoggedAccount();

    return <AuthProvider initialUser={loggedAccount}>
        {children}
    </AuthProvider>
}