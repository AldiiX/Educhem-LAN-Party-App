"use client";

import React, {ReactNode, useEffect, useRef, useState} from "react";
import {createPortal} from "react-dom";
import style from "./Modal.module.scss";

type ModalProps = {
    open: boolean;
    onClose: () => void;
    children: ReactNode;
    className?: string;
    closeOnBackdrop?: boolean;
};

const ANIMATION_MS = 220;

export function Modal({open, onClose, children, className, closeOnBackdrop = true}: ModalProps) {
    const [mounted, setMounted] = useState(open);
    const [closing, setClosing] = useState(false);
    const [body, setBody] = useState<HTMLElement | null>(null);
    const [renderedChildren, setRenderedChildren] = useState(children);
    const latestChildren = useRef(children);

    if(open) {
        latestChildren.current = children;
    }

    useEffect(() => {
        setBody(document.body);
    }, []);

    useEffect(() => {
        if(open) {
            setMounted(true);
            requestAnimationFrame(() => setClosing(false));
            return;
        }

        if(!mounted) return;

        setRenderedChildren(latestChildren.current);
        setClosing(true);
        const timeout = window.setTimeout(() => {
            setMounted(false);
            setClosing(false);
        }, ANIMATION_MS);

        return () => window.clearTimeout(timeout);
    }, [mounted, open]);

    useEffect(() => {
        if(!mounted) return;

        const onKeyDown = (event: KeyboardEvent) => {
            if(event.key === "Escape") onClose();
        };

        window.addEventListener("keydown", onKeyDown);
        return () => window.removeEventListener("keydown", onKeyDown);
    }, [mounted, onClose]);

    if(!mounted || !body) return null;

    return createPortal(
        <div className={`${style.layer} ${closing ? style.closing : ""}`} onMouseDown={event => {
            if(closeOnBackdrop && event.target === event.currentTarget) onClose();
        }}>
            <div className={`${style.content} ${className ?? ""}`} role="dialog" aria-modal="true">
                <button type="button" className={style.closeButton} onClick={onClose} aria-label="Zavřít"></button>
                {open ? children : renderedChildren}
            </div>
        </div>,
        body
    );
}
