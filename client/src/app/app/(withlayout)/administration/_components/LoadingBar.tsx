"use client";

import {useEffect, useState} from "react";
import style from "./LoadingBar.module.scss";

type LoadingBarProps = {
    active: boolean;
};

const loadingBarDelayMs = 150;

export function LoadingBar({active}: LoadingBarProps) {
    const [visible, setVisible] = useState(false);

    useEffect(() => {
        if(!active) {
            setVisible(false);
            return;
        }

        const timeout = window.setTimeout(() => setVisible(true), loadingBarDelayMs);
        return () => window.clearTimeout(timeout);
    }, [active]);

    return <div
        className={`${style.loadingBar} ${visible ? style.visible : ""}`}
        role="progressbar"
        aria-label="Načítání dat"
        aria-hidden={!visible}
    >
        <span></span>
    </div>;
}
