import style from "./page.module.scss";
import Link from "next/link"

export default function() {
    return <>
        <h1>About</h1>
        <Link href={"/"}>Home</Link>
    </>
}