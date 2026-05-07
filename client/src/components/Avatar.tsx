import React from "react";
import style from "./Avatar.module.scss";

const AVATAR_ENABLED: boolean = true;

type AvatarProps = {
    size?: string,
    name: string,
    src?: string | null,
    className?: string,
    background?: string,
    onClick?: () => void,
}

export const Avatar = ({size = "16px", src, className, background, name, onClick}: AvatarProps) => {
    if (! background) {
        // Výpočet hashe
        let hash = 0;
        for (let i = 0; i < name?.length; i++) {
            hash = name.charCodeAt(i) + ((hash << 5) - hash);
        }
        hash = Math.abs(hash);

        // Výpočet barevných složek
        let hue = hash % 360;
        let saturation = Math.floor((hash ?? 0) % 20 + 45);
        let lightness = Math.floor((hash ?? 0) % 20 + 45);


        // hardcoded colors
        if (name === "Stanislav Škudrna" || name === "Serhii Yavorskyi") {
            // fialova barva
            hue = 270;
            saturation = 50;
            lightness = 60;
        }


        // Vytvoření HSL barvy
        background = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
    }

    const letter = name?.split(" ").map((word) => word[0]).join("").slice(0, 2);

    return (
        <div onClick={onClick} className={style["avatar"] + (className ? " " + className : "")} style={{
            background: ! src || ! AVATAR_ENABLED ? background : "transparent",
            "--size": size
        } as React.CSSProperties}>
            {
                src && AVATAR_ENABLED ? (
                    <img className={style["image"]} src={src} alt="avatar"/>
                ) : (
                    <p className={style["letter"]}>{letter?.toUpperCase()}</p>
                )
            }
        </div>
    )
}