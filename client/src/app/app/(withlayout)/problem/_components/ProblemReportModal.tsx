import {Modal} from "@/components/Modal";
import {ProblemReportForm} from "./ProblemReportForm";
import style from "./ProblemReportModal.module.scss";
import type {ProblemReportHook} from "../_hooks/useProblemReport";

export function ProblemReportModal({report}: {report: ProblemReportHook}) {
    return <Modal open={report.isCreateModalOpen} onClose={report.closeCreateModal} className={style.modal}>
        <div className={style.header}>
            <span style={{maskImage: "url(/icons/warn2.svg)"}}></span>
            <div>
                <h2>Nová porucha</h2>
                <p>Vyplň hlášení tak, aby šlo rychle dohledat, kde a co se stalo.</p>
            </div>
        </div>
        <ProblemReportForm report={report} />
    </Modal>;
}
