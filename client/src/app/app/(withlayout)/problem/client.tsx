"use client";

import style from "./client.module.scss";
import {ProblemReportModal} from "./_components/ProblemReportModal";
import {ProblemReportsPanel} from "./_components/ProblemReportsPanel";
import {useProblemReport} from "./_hooks/useProblemReport";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {hasRoleAtLeast} from "@/lib/roles";

export default function ProblemClient() {
    const report = useProblemReport();
    const {account} = useAuth();
    const canManageReports = hasRoleAtLeast(account, "TeacherOrg");

    return <main className={style.problem}>
        <h1>Nahlásit problém</h1>

        <section className={style.intro}>
            <span style={{maskImage: "url(/icons/warn2.svg)"}}></span>
            <div>
                <h2>Dej organizátorům vědět, co nefunguje</h2>
                <p>Popiš problém co nejkonkrétněji, aby se dal rychle dohledat a vyřešit.</p>
            </div>
            <button type="button" onClick={report.openCreateModal} disabled={!report.reportsEnabled}>
                <span style={{maskImage: "url(/icons/plus.svg)"}}></span>
                Nová porucha
            </button>
        </section>

        {!report.reportsEnabled && <p className={style.success}>Hlášení problémů je momentálně uzamčené.</p>}

        {report.wasSubmitted && <p className={style.success}>Hlášení bylo odesláno.</p>}

        <ProblemReportsPanel report={report} canManageReports={canManageReports} />
        <ProblemReportModal report={report} />
    </main>;
}
