import type { ReactNode } from "react";
import styles from "./template.module.scss";

export default function Template({ children }: { children: ReactNode }) {
    return (
        <div className={styles.routeTransition}>
            {children}
        </div>
    );
}
