import type {Metadata} from "next";
import ResetPasswordClient from "./client";

export const metadata: Metadata = {
    title: "Reset hesla",
};

export default async function ResetPasswordPage({searchParams}: {searchParams: Promise<{token?: string}>}) {
    const params = await searchParams;
    return <ResetPasswordClient token={params.token ?? ""} />;
}
