import type {Metadata} from "next";
import {Logs} from "./client";

export const metadata: Metadata = {
    title: "Bezpečnostní logy • Administrace",
};

export default function AdministrationLogsPage() {
    return <Logs />;
}


