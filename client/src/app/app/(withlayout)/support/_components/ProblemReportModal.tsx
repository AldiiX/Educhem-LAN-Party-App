import {Modal} from "@/components/Modal";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {phrase} from "@/lib/communicationStyle";
import {ProblemReportForm} from "./ProblemReportForm";
import style from "./ProblemReportModal.module.scss";
import type {ProblemReportHook} from "../_hooks/useProblemReport";

export function ProblemReportModal({report}: {report: ProblemReportHook}) {
    const {account} = useAuth();

    return <Modal open={report.isCreateModalOpen} onClose={report.closeCreateModal} className={style.modal}>
        <div className={style.header}>
            <span style={{maskImage: "url(/icons/warn2.svg)"}}></span>
            <div>
                <h2>Nová porucha</h2>
                <p>{phrase(
                    account?.communicationStyle,
                    "Vyplň hlášení tak, aby šlo rychle dohledat, kde a co se stalo.",
                    "Vyplňte hlášení tak, aby šlo rychle dohledat, kde a co se stalo."
                )}</p>
            </div>
        </div>
        <ProblemReportForm report={report} />
    </Modal>;
}
