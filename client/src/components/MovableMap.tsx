"use client";

import { type ReactNode, useCallback, useEffect, useRef, useState } from "react";
import styles from "./MovableMap.module.scss";

type Point = {
    x: number;
    y: number;
};

type MovableMapProps = {
    width?: number;
    height?: number;
    initialScale?: number;
    minScale?: number;
    maxScale?: number;
    showControls?: boolean;
    resetKey?: string;
    className?: string;
    topLeft?: ReactNode;
    bottomLeft?: ReactNode;
    bottomRight?: ReactNode;
    children?: ReactNode;
};

const DEFAULT_WIDTH = 1600;
const DEFAULT_HEIGHT = 900;
const BUTTON_ZOOM_STEP = 0.075;
const WHEEL_ZOOM_STEP = 0.06;

export default function MovableMap({
    width = DEFAULT_WIDTH,
    height = DEFAULT_HEIGHT,
    initialScale = 0.65,
    minScale = 0.35,
    maxScale = 1.8,
    showControls = true,
    resetKey,
    className,
    topLeft,
    bottomLeft,
    bottomRight,
    children,
}: MovableMapProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const svgRef = useRef<SVGSVGElement>(null);
    const activePointerId = useRef<number | null>(null);
    const dragStart = useRef<Point>({ x: 0, y: 0 });
    const dragStartOffset = useRef<Point>({ x: 0, y: 0 });

    const [containerCenter, setContainerCenter] = useState<Point>({ x: 0, y: 0 });
    const [containerSize, setContainerSize] = useState<Point>({ x: width, y: height });
    const [offset, setOffset] = useState<Point>({ x: 0, y: 0 });
    const [scale, setScale] = useState(initialScale);
    const [isDragging, setIsDragging] = useState(false);

    const clampScale = useCallback(
        (value: number) => Math.min(maxScale, Math.max(minScale, value)),
        [maxScale, minScale],
    );

    const zoomTo = useCallback(
        (nextScale: number, anchor?: Point) => {
            setScale((currentScale) => {
                const normalizedScale = clampScale(nextScale);
                const ratio = normalizedScale / currentScale;
                const zoomAnchor = anchor ?? containerCenter;

                setOffset((currentOffset) => ({
                    x: ratio * currentOffset.x + (1 - ratio) * (zoomAnchor.x - containerCenter.x),
                    y: ratio * currentOffset.y + (1 - ratio) * (zoomAnchor.y - containerCenter.y),
                }));

                return normalizedScale;
            });
        },
        [clampScale, containerCenter],
    );

    const resetView = useCallback(() => {
        setScale(initialScale);
        setOffset({ x: 0, y: 0 });
    }, [initialScale]);

    useEffect(() => {
        resetView();
    }, [resetKey, resetView]);

    useEffect(() => {
        const updateCenter = () => {
            const container = containerRef.current;
            if (!container) return;

            const nextSize = {
                x: container.clientWidth,
                y: container.clientHeight,
            };

            setContainerSize(nextSize);
            setContainerCenter({
                x: nextSize.x / 2,
                y: nextSize.y / 2,
            });
        };

        updateCenter();

        const container = containerRef.current;
        if (!container || !("ResizeObserver" in window)) {
            window.addEventListener("resize", updateCenter);
            return () => window.removeEventListener("resize", updateCenter);
        }

        const observer = new ResizeObserver(updateCenter);
        observer.observe(container);

        return () => observer.disconnect();
    }, []);

    useEffect(() => {
        const svg = svgRef.current;
        const container = containerRef.current;
        if (!svg || !container) return;

        const handleWheel = (event: WheelEvent) => {
            event.preventDefault();

            const rect = container.getBoundingClientRect();
            const anchor = {
                x: event.clientX - rect.left,
                y: event.clientY - rect.top,
            };
            const direction = event.deltaY < 0 ? 1 : -1;

            zoomTo(scale + direction * WHEEL_ZOOM_STEP, anchor);
        };

        svg.addEventListener("wheel", handleWheel, { passive: false });

        return () => svg.removeEventListener("wheel", handleWheel);
    }, [scale, zoomTo]);

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const preventPageScroll = (event: TouchEvent) => {
            event.preventDefault();
        };

        container.addEventListener("touchmove", preventPageScroll, { passive: false });

        return () => container.removeEventListener("touchmove", preventPageScroll);
    }, []);

    const handlePointerDown = (event: React.PointerEvent<SVGSVGElement>) => {
        if (event.button !== 0 && event.pointerType === "mouse") return;

        activePointerId.current = event.pointerId;
        dragStart.current = { x: event.clientX, y: event.clientY };
        dragStartOffset.current = offset;
        setIsDragging(true);

        event.currentTarget.setPointerCapture(event.pointerId);
    };

    const handlePointerMove = (event: React.PointerEvent<SVGSVGElement>) => {
        if (activePointerId.current !== event.pointerId) return;

        setOffset({
            x: dragStartOffset.current.x + event.clientX - dragStart.current.x,
            y: dragStartOffset.current.y + event.clientY - dragStart.current.y,
        });
    };

    const endDrag = (event: React.PointerEvent<SVGSVGElement>) => {
        if (activePointerId.current !== event.pointerId) return;

        activePointerId.current = null;
        setIsDragging(false);

        if (event.currentTarget.hasPointerCapture(event.pointerId)) {
            event.currentTarget.releasePointerCapture(event.pointerId);
        }
    };

    return (
        <div
            ref={containerRef}
            className={[styles.map, className].filter(Boolean).join(" ")}
        >
            {topLeft && <div className={`${styles.overlay} ${styles.topLeft}`}>{topLeft}</div>}
            {bottomLeft && <div className={`${styles.overlay} ${styles.bottomLeft}`}>{bottomLeft}</div>}
            {bottomRight && <div className={`${styles.overlay} ${styles.bottomRight}`}>{bottomRight}</div>}

            {showControls && (
                <div className={styles.controls} aria-label="Map controls">
                    <button type="button" onClick={() => zoomTo(scale + BUTTON_ZOOM_STEP)} aria-label="Zoom in">
                        +
                    </button>
                    <button type="button" onClick={() => zoomTo(scale - BUTTON_ZOOM_STEP)} aria-label="Zoom out">
                        -
                    </button>
                </div>
            )}

            <svg
                ref={svgRef}
                className={styles.svg}
                width="100%"
                height="100%"
                viewBox={`0 0 ${containerSize.x} ${containerSize.y}`}
                onPointerDown={handlePointerDown}
                onPointerMove={handlePointerMove}
                onPointerUp={endDrag}
                onPointerCancel={endDrag}
                onLostPointerCapture={() => {
                    activePointerId.current = null;
                    setIsDragging(false);
                }}
                style={{ cursor: isDragging ? "grabbing" : "grab" }}
            >
                <g
                    transform={`translate(${containerCenter.x + offset.x} ${containerCenter.y + offset.y}) scale(${scale}) translate(${-width / 2} ${-height / 2})`}
                >
                    {children ?? (
                        <>
                            <rect width={width} height={height} rx="24" fill="var(--background-bg-2)" />
                            <text x="50%" y="50%" dominantBaseline="middle" textAnchor="middle" fontSize="48" fill="var(--text-color)">
                                Map Content
                            </text>
                        </>
                    )}
                </g>
            </svg>
        </div>
    );
}
