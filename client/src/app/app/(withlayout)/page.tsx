import {z} from "zod";
import {fetchBackendJson} from "@/lib/backendClient";
import {getCurrentLoggedAccount} from "@/lib/auth";
import HomeClient from "./_components/HomeClient";

const DashboardSchema = z.object({
    totalAccounts: z.number(),
    activeNow: z.number(),
    activeToday: z.number(),
    reservationsEnabled: z.number(),
    staffCount: z.number(),
    latestAccounts: z.array(z.object({
        fullName: z.string(),
        class: z.string().nullish(),
        createdAtUtc: z.coerce.date(),
    })),
    classBreakdown: z.array(z.object({
        class: z.string(),
        count: z.number(),
    })),
});

export type HomeDashboard = z.infer<typeof DashboardSchema>;

export default async function HomePage() {
    const account = await getCurrentLoggedAccount();
    let dashboard: HomeDashboard | null = null;

    try {
        const response = await fetchBackendJson<unknown>("/api/v1/account/dashboard", {method: "GET"});
        const parsed = DashboardSchema.safeParse(response);
        if(parsed.success) dashboard = parsed.data;
    } catch {
        dashboard = null;
    }

    return <HomeClient account={account} dashboard={dashboard} />;
}
