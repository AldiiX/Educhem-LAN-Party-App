import type {Metadata} from "next";
import {ForumPosts} from "./client";
import {redirect} from "next/navigation";

export const metadata: Metadata = {
    title: "Forum příspěvky • Administrace",
};

export default function AdministrationForumPage() {
    redirect("/app/administration/");
}
