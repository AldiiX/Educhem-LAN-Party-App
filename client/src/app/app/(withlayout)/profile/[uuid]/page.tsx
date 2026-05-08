import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { cache } from "react";

import { fetchBackendJson } from "@/lib/backendClient";
import { AccountSchema } from "@/schemas/AccountSchema";
import Client from "@/app/app/(withlayout)/profile/client";
import {getCachedCurrentLoggedAccount} from "@/lib/auth";

type PageProps = {
    params: Promise<{
        uuid: string;
    }>;
};

const getProfile = cache(async (uuid: string) => {
    try {
        const res = await fetchBackendJson<unknown>(`/api/v1/profile/${uuid}`);
        const profile = AccountSchema.safeParse(res);

        if(!profile.success) {
            return null;
        }

        return profile.data;
    } catch {
        return null;
    }
});

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
    const { uuid } = await params;
    const profile = await getProfile(uuid);

    if(!profile) {
        return {
            title: "Profil nenalezen",
        };
    }

    return {
        title: profile.fullName + ' - Profil',
        //icons: profile.avatarUrl
    };
}

export default async function Page({ params }: PageProps) {
    const account = await getCachedCurrentLoggedAccount();

    const { uuid } = await params;
    const profile = await getProfile(uuid);

    if(!profile) {
        notFound();
    }

    if(profile.id == account?.id) redirect("/app/profile")

    return <Client account={profile} />;
}