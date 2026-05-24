import NotFound from "@/app/not-found";
import { Metadata } from "next";

export const metadata: Metadata = {
    title: "404 - Stránka nenalezena"
}

export default function() {
    return <>
        <NotFound />
    </>
}