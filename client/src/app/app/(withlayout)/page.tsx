import {z} from "zod";
import {fetchBackendJson} from "@/lib/backendClient";
import {getCurrentLoggedAccount} from "@/lib/auth";
import HomeClient from "./HomeClient";
import {AccountSchema} from "@/schemas/AccountSchema";
import {StatusData} from "@/app/app/(withlayout)/_hooks/useStatus";

const DashboardSchema = z.object({
    totalAccounts: z.number(),
    activeNow: z.number(),
    activeToday: z.number(),
    reservationsEnabled: z.number(),
    staffCount: z.number(),
    latestAccounts: z.array(AccountSchema),
    classBreakdown: z.array(z.object({
        class: z.string(),
        count: z.number(),
    })),
});

export type HomeDashboard = z.infer<typeof DashboardSchema>;

export default async function HomePage() {
    const account = await getCurrentLoggedAccount();

    const dashboardPromise = fetchBackendJson<unknown>("/api/v1/account/dashboard", { method: "GET" })
        .then(response => {
            const parsed = DashboardSchema.safeParse(response);
            return parsed.success ? parsed.data : null;
        })
        .catch(() => null);

    const statusPromise = fetchBackendJson<StatusData>("/api/v1/reservations/status", { method: "GET" })
        .catch(() => null);

    const [dashboard, status] = await Promise.all([
        dashboardPromise,
        statusPromise
    ]);

    return <HomeClient account={account} dashboard={dashboard} status={status} />;
}
