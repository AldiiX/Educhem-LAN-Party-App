import style from "./EmptyAnnouncements.module.scss";

export function EmptyAnnouncements() {
    return <section className={style.empty}>
        <span style={{maskImage: "url(/icons/bell.svg)"}}></span>
        <h2>Nebyly nalezeny žádné zprávy</h2>
    </section>;
}
