import {AccountGender} from "@/schemas/AccountSchema";

export function translateGender(gender?: AccountGender | null) {
    switch (gender) {
        case "Male":
            return "Muž";
        case "Female":
            return "Žena";
        case "Other":
            return "Jiné";
        default:
            return "Nezadané";
    }
}