import {Header} from "@/components/header";
import {Footer} from "@/components/footer";
import styles from "./layout.module.scss";

export default function ({ children }: { children: React.ReactNode }) {
    return <div className={styles.shell}>
        <Header/>
        {children}
        <Footer/>
    </div>;
}
