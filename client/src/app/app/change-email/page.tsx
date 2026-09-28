import type {Metadata} from "next";
import ChangeEmailClient from "./client";

export const metadata: Metadata = {
    title: "Změna e-mailu",
    robots: {index: false, follow: false},
    referrer: "no-referrer",
};

export default function ChangeEmailPage() {
    return <ChangeEmailClient />;
}
