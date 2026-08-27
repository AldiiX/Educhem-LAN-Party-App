import type {Metadata} from "next";
import {Users} from "./client";

export const metadata: Metadata = {
    title: "Uživatelé • Administrace",
};

export default function AdministrationUsersPage() {
    return <Users />;
}