"use client";

import Link from "next/link";
import {usePathname} from "next/navigation";
import {FocusEvent as ReactFocusEvent, MouseEvent as ReactMouseEvent, ReactNode, useEffect, useRef, useState, useSyncExternalStore} from "react";
import {createPortal} from "react-dom";
import {Account} from "@/schemas/AccountSchema";
import {accountTypeLabel, genderLabel, schoolLabel} from "@/lib/enumLabels";
import {Avatar} from "@/components/Avatar";
import styles from "./ProfileHoverCard.module.scss";

type ProfileHoverCardProps = {
    account: Account;
    children: ReactNode;
    className?: string;
};

type PopupPosition = {
    top: number;
    left: number;
    placement: "right" | "left" | "bottom";
};

type ActiveProfileHover = {
    id: string;
    account: Account;
    rect: DOMRect;
};

type ProfileHoverSnapshot = {
    active: ActiveProfileHover | null;
    closing: boolean;
};

const CARD_WIDTH = 300;
const CARD_HEIGHT = 400;
const GAP = 12;
const CLOSE_ANIMATION_MS = 150;

let activeProfileHover: ActiveProfileHover | null = null;
let profileHoverClosing = false;
let profileHoverSnapshot: ProfileHoverSnapshot = {
    active: activeProfileHover,
    closing: profileHoverClosing,
};
let closeTimer: ReturnType<typeof setTimeout> | null = null;
let closeAnimationTimer: ReturnType<typeof setTimeout> | null = null;
let overlayOwner: symbol | null = null;
const profileHoverListeners = new Set<() => void>();

function subscribeProfileHover(listener: () => void) {
    profileHoverListeners.add(listener);
    return () => profileHoverListeners.delete(listener);
}

function getProfileHoverSnapshot() {
    return profileHoverSnapshot;
}

function emitProfileHoverChange() {
    profileHoverSnapshot = {
        active: activeProfileHover,
        closing: profileHoverClosing,
    };

    profileHoverListeners.forEach(listener => listener());
}

function clearProfileHoverTimers() {
    if(closeTimer) clearTimeout(closeTimer);
    if(closeAnimationTimer) clearTimeout(closeAnimationTimer);
    closeTimer = null;
    closeAnimationTimer = null;
}

export function closeProfileHoverImmediate() {
    clearProfileHoverTimers();

    if(!activeProfileHover && !profileHoverClosing) return;

    activeProfileHover = null;
    profileHoverClosing = false;
    emitProfileHoverChange();
}

function openProfileHover(next: ActiveProfileHover) {
    clearProfileHoverTimers();
    activeProfileHover = next;
    profileHoverClosing = false;
    emitProfileHoverChange();
}

function cancelProfileHoverClose() {
    clearProfileHoverTimers();

    if(profileHoverClosing) {
        profileHoverClosing = false;
        emitProfileHoverChange();
    }
}

function scheduleProfileHoverClose() {
    if(closeTimer) clearTimeout(closeTimer);

    closeTimer = setTimeout(() => {
        profileHoverClosing = true;
        emitProfileHoverChange();

        closeAnimationTimer = setTimeout(() => {
            activeProfileHover = null;
            profileHoverClosing = false;
            emitProfileHoverChange();
        }, CLOSE_ANIMATION_MS);
    }, 90);
}

export function ProfileHoverCard({account, children, className}: ProfileHoverCardProps) {
    const anchorRef = useRef<HTMLSpanElement | null>(null);
    const [ownsOverlay, setOwnsOverlay] = useState(false);

    useEffect(() => {
        const owner = Symbol("ProfileHoverCardOverlay");

        if(!overlayOwner) {
            overlayOwner = owner;
            setOwnsOverlay(true);
        }

        return () => {
            if(overlayOwner === owner) {
                overlayOwner = null;
            }
        };
    }, []);

    const isOwnDomEvent = (event: ReactMouseEvent<HTMLSpanElement> | ReactFocusEvent<HTMLSpanElement>) => {
        return event.target instanceof Node && event.currentTarget.contains(event.target);
    };

    const show = () => {
        const anchor = anchorRef.current;
        if(!anchor) return;

        const target = anchor.firstElementChild ?? anchor;
        openProfileHover({
            id: account.id,
            account,
            rect: target.getBoundingClientRect(),
        });
    };

    return <span
        ref={anchorRef}
        className={className ? `${styles.anchor} ${className}` : styles.anchor}
        onMouseEnter={(event) => {
            if(isOwnDomEvent(event)) show();
        }}
        onMouseLeave={(event) => {
            if(isOwnDomEvent(event)) scheduleProfileHoverClose();
        }}
        onFocusCapture={(event) => {
            if(isOwnDomEvent(event)) show();
        }}
        onBlurCapture={(event) => {
            if(isOwnDomEvent(event)) scheduleProfileHoverClose();
        }}
    >
        {children}
        {ownsOverlay && <ProfileHoverCardOverlay />}
    </span>;
}

function ProfileHoverCardOverlay() {
    const [mounted, setMounted] = useState(false);
    const [position, setPosition] = useState<PopupPosition | null>(null);
    const snapshot = useSyncExternalStore(subscribeProfileHover, getProfileHoverSnapshot, getProfileHoverSnapshot);
    const active = snapshot.active;
    const pathname = usePathname();

    useEffect(() => {
        setMounted(true);
    }, []);

    useEffect(() => {
        closeProfileHoverImmediate();
    }, [pathname]);

    useEffect(() => {
        if(!active) return;

        const updatePosition = () => {
            const rect = active.rect;
            const viewportWidth = window.innerWidth;
            const viewportHeight = window.innerHeight;
            const preferRight = rect.right + GAP + CARD_WIDTH <= viewportWidth - 12;
            const preferLeft = rect.left - GAP - CARD_WIDTH >= 12;
            const placement = preferRight ? "right" : preferLeft ? "left" : "bottom";
            const left = placement === "right"
                ? rect.right + GAP
                : placement === "left"
                    ? rect.left - GAP - CARD_WIDTH
                    : rect.left;
            const top = placement === "bottom" ? rect.bottom + GAP : rect.top;

            setPosition({
                placement,
                left: Math.min(Math.max(12, left), viewportWidth - CARD_WIDTH - 12),
                top: Math.min(Math.max(12, top), viewportHeight - CARD_HEIGHT - 12),
            });
        };

        updatePosition();
        window.addEventListener("resize", updatePosition);
        window.addEventListener("scroll", updatePosition, true);

        return () => {
            window.removeEventListener("resize", updatePosition);
            window.removeEventListener("scroll", updatePosition, true);
        };
    }, [active]);

    if(!mounted || !active || !position) return null;

    return createPortal(
        <ProfileHoverCardContent
            account={active.account}
            closing={snapshot.closing}
            position={position}
            onMouseEnter={cancelProfileHoverClose}
            onMouseLeave={scheduleProfileHoverClose}
        />,
        document.body,
    );
}

function ProfileHoverCardContent({account, closing, position, onMouseEnter, onMouseLeave}: {
    account: Account;
    closing: boolean;
    position: PopupPosition;
    onMouseEnter: () => void;
    onMouseLeave: () => void;
}) {
    const achievements = (account.achievements ?? []).filter(entry => !entry.isHidden);
    const badges = (account.badges ?? []).filter(entry => entry.isTakenOut).slice(0, 3);
    const classText = account.class ?? "Bez třídy";
    const schoolText = account.school?.shortName || account.school?.displayName || "Bez školy";

    return <div
        className={`${styles.card} ${styles[position.placement]} ${closing ? styles.closing : ""}`}
        style={{top: position.top, left: position.left}}
        onMouseEnter={onMouseEnter}
        onMouseLeave={onMouseLeave}
    >
        <div
            className={account.bannerUrl ? styles.banner : `${styles.banner} ${styles.emptyBanner}`}
            style={account.bannerUrl ? {backgroundImage: `url(${account.bannerUrl})`} : undefined}
        >
            <Avatar name={account.fullName} src={account.avatarUrl} size="76px" className={styles.avatar} />
        </div>

        <div className={styles.body}>
            <div className={styles.header}>
                <div>
                    <h3>{account.fullName}</h3>
                    <p>{accountTypeLabel(account.accountType, account.gender)}</p>
                </div>
                
                {badges.length > 0 && (
                    <div className={styles.badges}>
                        {badges.map(entry => (
                            <span key={entry.id} title={entry.badge.name}>
                                {entry.badge.iconUrl ? <img src={entry.badge.iconUrl} alt="" /> : entry.badge.name.charAt(0)}
                            </span>
                        ))}
                    </div>
                )}
            </div>

            <div className={styles.infoGrid}>
                <InfoItem icon="/icons/class.svg" label="Třída" value={classText} />
                <InfoItem icon="/icons/organization.svg" label="Škola" value={schoolText} image={account.school?.iconUrl} title={account.school?.displayName ?? "Bez školy"} />
                {account.gender && <InfoItem icon="/icons/gender.svg" label="Pohlaví" value={genderLabel(account.gender)} />}
                <InfoItem icon="/icons/login.svg" label="Registrace" value={account.createdAtUtc.toLocaleDateString("cs-CZ")} />
            </div>

            <Link href={`/app/profile/${account.id}`} className={styles.profileLink}>Otevřít profil</Link>
        </div>
    </div>;
}

function InfoItem({icon, label, value, image, title}: {icon: string; label: string; value: string; image?: string | null, title?: string | null}) {
    return <div className={styles.infoItem} title={title ?? ""}>
        {image ? <img src={image} alt="" /> : <span style={{maskImage: `url(${icon})`}} />}
        <div>
            <small>{label}</small>
            <p>{value}</p>
        </div>
    </div>;
}
