import React from 'react';
import {createPortal} from 'react-dom';

export default function Teleport({children}: { children?: React.ReactNode }) {
    return createPortal(
        children,
        document.getElementById("teleports") as HTMLElement
    );
}