import type {ReactNode} from "react";
import AdministrationClient from "./client";

export default function AdministrationLayout({children}: {children: ReactNode}) {
    return <AdministrationClient>{children}</AdministrationClient>;
}
