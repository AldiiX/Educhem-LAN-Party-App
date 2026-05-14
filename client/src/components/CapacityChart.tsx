"use client";

import { type CSSProperties, useEffect, useState } from "react";
import styles from "./CapacityChart.module.scss";

type CapacityChartProps = {
    percentage: number;
    label?: string;
    className?: string;
};

export default function CapacityChart({
    percentage,
    label = "Naplněné kapacity",
    className,
}: CapacityChartProps) {
    const normalizedPercentage = Math.min(100, Math.max(0, Math.round(percentage)));
    const [animatedPercentage, setAnimatedPercentage] = useState(0);
    const chartStyle = {"--filled-capacity": `${animatedPercentage}%`} as CSSProperties;

    useEffect(() => {
        const animationFrame = requestAnimationFrame(() => {
            setAnimatedPercentage(normalizedPercentage);
        });

        return () => cancelAnimationFrame(animationFrame);
    }, [normalizedPercentage]);

    return (
        <div className={[styles.capacityChart, className].filter(Boolean).join(" ")}>
            <div className={styles.ring} style={chartStyle} aria-hidden="true" />
            <div>
                <strong>{normalizedPercentage}%</strong>
                <span>{label}</span>
            </div>
        </div>
    );
}
