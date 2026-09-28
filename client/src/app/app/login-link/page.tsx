import type {Metadata} from "next";
import LoginLinkClient from "./client";

export const metadata: Metadata = {
    title: "Potvrzení přihlášení",
    robots: {index: false, follow: false},
    referrer: "no-referrer",
};

export default function LoginLinkPage() {
    return <LoginLinkClient />;
}
