export function toTimestamp(value: unknown) {
    if (value instanceof Date) {
        return value.getTime();
    }

    if (typeof value === "string" || typeof value === "number") {
        return new Date(value).getTime();
    }

    return Number.NaN;
}
