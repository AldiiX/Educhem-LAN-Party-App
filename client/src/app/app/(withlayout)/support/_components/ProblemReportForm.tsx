import style from "./ProblemReportForm.module.scss";
import type {ProblemReportHook} from "../_hooks/useProblemReport";
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {phrase} from "@/lib/communicationStyle";

export function ProblemReportForm({report}: {report: ProblemReportHook}) {
    const {account} = useAuth();
    const isFemale = account?.gender === "Female";
    const descriptionPlaceholder = phrase(
        account?.communicationStyle,
        isFemale ? "Co se stalo, kde se to stalo a co jsi zkoušela?" : "Co se stalo, kde se to stalo a co jsi zkoušel?",
        "Co se stalo, kde se to stalo a co jste zkoušeli?"
    );

    return <form className={style.form} onSubmit={report.submit}>
        {report.submitError && <p className={style.error}>{report.submitError}</p>}

        <div className={style.row}>
            <label>
                <span>Kategorie</span>
                <select value={report.form.category} onChange={event => report.updateField("category", event.target.value)}>
                    {report.categoryOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                </select>
            </label>

            <label>
                <span>Priorita</span>
                <select value={report.form.priority} onChange={event => report.updateField("priority", event.target.value)}>
                    {report.priorityOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                </select>
            </label>
        </div>

        <label>
            <span>Nadpis</span>
            <input
                value={report.form.title}
                onChange={event => report.updateField("title", event.target.value)}
                placeholder="Např. nejde se přihlásit"
            />
        </label>

        <label>
            <span>Popis problému</span>
            <textarea
                value={report.form.description}
                onChange={event => report.updateField("description", event.target.value)}
                placeholder={descriptionPlaceholder}
                rows={7}
            />
        </label>

        <label>
            <span>Kontakt</span>
            <input
                value={report.form.contact}
                onChange={event => report.updateField("contact", event.target.value)}
                placeholder="Discord, e-mail nebo telefon"
            />
        </label>

        <button type="submit" disabled={!report.canSubmit || report.isSubmitting || !report.reportsEnabled}>
            {report.isSubmitting ? "Odesílám..." : "Odeslat hlášení"}
        </button>
    </form>;
}
