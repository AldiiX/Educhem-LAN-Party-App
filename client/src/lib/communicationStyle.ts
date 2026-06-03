import type {AccountCommunicationStyle} from "@/schemas/AccountSchema";

export function isFormalCommunication(style?: AccountCommunicationStyle | null) {
    return style !== "Informal";
}

export function phrase(style: AccountCommunicationStyle | null | undefined, informal: string, formal: string) {
    return isFormalCommunication(style) ? formal : informal;
}
