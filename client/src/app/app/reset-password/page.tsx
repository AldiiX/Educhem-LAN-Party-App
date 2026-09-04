import type {Metadata} from "next";
import ResetPasswordClient from "./client";

export const metadata: Metadata = {
    title: "Reset hesla",
    robots: {index: false, follow: false},
    referrer: "no-referrer",
};

export default function ResetPasswordPage() {
    return <ResetPasswordClient />;
}
