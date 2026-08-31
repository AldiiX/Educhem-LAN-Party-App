import AccountAchievements from "./client";
import type {Metadata} from "next";

export const metadata: Metadata = {
    title: "Mé achievementy",
};

export default function AccountAchievementsPage() {
    return <AccountAchievements/>;
}
