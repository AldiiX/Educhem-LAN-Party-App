"use client";

import style from "./ModalDialog.module.scss";
import {Modal} from "@/components/Modal";

type ModalDialogProps = {
    open: boolean;
    title: string;
    description: string;
    confirmText?: string;
    cancelText?: string;
    loading?: boolean;
    onClose: () => void;
    onConfirm?: () => void;
};

export function ModalDestructive({
                                     open,
                                     title,
                                     description,
                                     confirmText = "Ano",
                                     cancelText = "Ne",
                                     loading = false,
                                     onClose,
                                     onConfirm,
                                 }: ModalDialogProps) {
    return <Modal open={open} onClose={onClose} className={`${style.dialog} ${style.destructiveDialog}`}>
        <DialogContent
            title={title}
            description={description}
            confirmText={confirmText}
            cancelText={cancelText}
            loading={loading}
            destructive
            onClose={onClose}
            onConfirm={onConfirm}
        />
    </Modal>;
}

export function ModalInformative({
                                     open,
                                     title,
                                     description,
                                     confirmText = "Rozumím",
                                     cancelText = "Zavřít",
                                     loading = false,
                                     onClose,
                                     onConfirm,
                                 }: ModalDialogProps) {
    return <Modal open={open} onClose={onClose} className={`${style.dialog} ${style.informativeDialog}`}>
        <DialogContent
            title={title}
            description={description}
            confirmText={confirmText}
            cancelText={cancelText}
            loading={loading}
            onClose={onClose}
            onConfirm={onConfirm}
        />
    </Modal>;
}

function DialogContent({
                           title,
                           description,
                           confirmText,
                           cancelText,
                           loading,
                           destructive,
                           onClose,
                           onConfirm,
                       }: Required<Pick<ModalDialogProps, "title" | "description" | "confirmText" | "cancelText" | "loading" | "onClose">> & {
    destructive?: boolean;
    onConfirm?: () => void;
}) {
    const confirm = onConfirm ?? onClose;

    return <div className={style.content}>
        <div className={style.icon} aria-hidden="true"></div>
        <h2>{title}</h2>
        <p>{description}</p>
        <div className={style.actions}>
            {destructive && <button type="button" className={style.cancel} onClick={onClose}>{cancelText}</button>}
            <button type="button" className={style.primary} disabled={loading} onClick={confirm}>
                {loading ? "Pracuji..." : confirmText}
            </button>
        </div>
    </div>;
}
