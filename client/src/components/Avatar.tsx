import React, {useEffect, useMemo, useState} from "react";
import style from "./Avatar.module.scss";

const AVATAR_ENABLED: boolean = true;

type AvatarProps = {
    size?: string,
    name: string,
    src?: string | null,
    className?: string,
    background?: string,
    fallbackOnImageError?: boolean,
    onClick?: () => void,
}

// vypocet stabilniho hashe podle jmena
const getAvatarBackground = (name: string): string => {
    let hash = 1509_0611_6767;

    for(const char of name) {
        hash ^= char.codePointAt(0) ?? 0;
        hash = Math.imul(hash, 16777619);
    }

    hash >>>= 0;

    let hue = hash % 360;
    let saturation = 30 + ((hash >>> 8) % 45);
    let lightness = 40 + ((hash >>> 16) % 25);

    // hardcoded colors
    /*if(name === "Stanislav Škudrna" || name === "Serhii Yavorskyi") {
        // fialova barva
        hue = 270;
        saturation = 58;
        lightness = 58;
    }*/

    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
}

const getAvatarLetter = (name: string): string => {
    const words = name.trim().split(/\s+/).filter(Boolean);
    const initials = words.map((word) => word[0]).join("").slice(0, 2);

    return initials.toUpperCase();
}

export function Avatar({
                           size = "16px",
                           src,
                           className,
                           background,
                           fallbackOnImageError = true,
                           name,
                           onClick
                       }: AvatarProps
) {
    const [imageFailed, setImageFailed] = useState(false);

    useEffect(() => {
        setImageFailed(false);
    }, [src, fallbackOnImageError]);

    const avatarBackground = useMemo(() => {
        return background ?? getAvatarBackground(name);
    }, [background, name]);

    const letter = useMemo(() => {
        return getAvatarLetter(name);
    }, [name]);

    const showImage = Boolean(src && AVATAR_ENABLED && (! imageFailed || ! fallbackOnImageError));

    return (
        <div
            onClick={onClick}
            className={style.avatar + (className ? " " + className : "")}
            style={{
                background: showImage ? "transparent" : avatarBackground,
                "--size": size
            } as React.CSSProperties}
        >
            {
                showImage ? (
                    <img
                        className={style.image}
                        src={src ?? undefined}
                        alt="avatar"
                        onError={() => {
                            if(fallbackOnImageError) {
                                setImageFailed(true);
                            }
                        }}
                    />
                ) : (
                    <p className={style.letter}>{letter}</p>
                )
            }
        </div>
    )
}