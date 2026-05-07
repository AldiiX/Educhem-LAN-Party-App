import s from "./Button.module.scss";
import { ButtonHTMLAttributes } from "react";

export type ButtonProps = {
    text?: string;
    icon?: string | null;
    onClick?: () => void;
    className?: string;
    type: "primary" | "secondary" | "tertiary" | "tertiary-rich";
    form?: string | null;
    buttonType?: "button" | "submit" | "reset";
    name?: string | null;
    disabled?: boolean;
    loading?: boolean;
    style?: React.CSSProperties;
}

export const Button = ({
                           text = "Odeslat",
                           icon = null,
                           onClick = () => {},
                           type,
                           className = "",
                           form = null,
                           buttonType = "button",
                           name = null,
                           disabled = false,
                           loading = false,
    style = {},
}: ButtonProps) => {
    if(loading) {
        return (
            <button className={`${s.btn} ${s["button-" + type]} ${className} ${s["button-loading"]}`} disabled style={style}>
                <div className={s["loading-spinner"]}></div>
                &nbsp;
            </button>
        );
    }

    return (
        <button className={`${s.btn} ${s["button-" + type]} ${className}`} onClick={onClick} {...form ? { form: form } : {}} type={buttonType} { ...name ? { name: name } : {} } { ...disabled ? { disabled: disabled } : {} } style={style}>
            {
                icon ?
                    <div className={s.icon} style={{ maskImage: `url(${icon})`}}></div>
                : null
            }

            { text }
        </button>
    );
}