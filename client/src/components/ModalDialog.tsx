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
                                     confirmText = "Smazat",
                                     cancelText = "Zrušit",
                                     loading = false,
                                     onClose,
                                     onConfirm,
                                 }: ModalDialogProps) {
    return <Modal open={open} onClose={onClose} className={style.dialog}>
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
    </Modal>
}

export function ModalInformative({
                                     open,
                                     title,
                                     description,
                                     confirmText = "Potvrdit",
                                     cancelText = "Zrušit",
                                     loading = false,
                                     onClose,
                                     onConfirm,
                                 }: ModalDialogProps) {
    return <Modal open={open} onClose={onClose} className={style.dialog}>
        <DialogContent
            title={title}
            description={description}
            confirmText={confirmText}
            cancelText={cancelText}
            loading={loading}
            onClose={onClose}
            onConfirm={onConfirm}
        />
    </Modal>
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
    return <div className={style.content}>
        <h2>{title}</h2>
        <p>{description}</p>
        <div className={style.actions}>
            <button type="button" onClick={onClose}>{cancelText}</button>
            {onConfirm && <button type="button" className={destructive ? style.destructive : style.primary} disabled={loading} onClick={onConfirm}>
                {destructive && <span style={{maskImage: "url(/icons/trash.svg)"}}></span>}
                {loading ? "Pracuji..." : confirmText}
            </button>}
        </div>
    </div>
}
