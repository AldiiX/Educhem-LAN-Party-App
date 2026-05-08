import {Account, AccountGender, AccountType} from "@/schemas/AccountSchema";

export function accountTypeLabel(type?: AccountType | null, gender?: AccountGender | null) {
    const female = gender === "Female";

    switch (type) {
        case "Admin":
            return female ? "Administrátorka" : "Administrátor";
        case "SuperAdmin":
            return female ? "Administrátorka (SU)" : "Administrátor (SU)";
        case "Teacher":
            return female ? "Učitelka" : "Učitel";
        case "TeacherOrg":
            return female ? "Učitelka (ORG)" : "Učitel (ORG)";
        case "Student":
            return female ? "Studentka" : "Student";
        default:
            return female ? "Neznámá" : "Neznámý";
    }
}

export function accountTypeFilterLabel(type: string) {
    return accountTypeLabel(type as AccountType);
}

export function genderLabel(gender?: AccountGender | null) {
    switch (gender) {
        case "Female":
            return "Žena";
        case "Male":
            return "Muž";
        case "Other":
            return "Ostatní";
        default:
            return "Neznámé";
    }
}

export function schoolLabel(school?: Account["school"] | null) {
    if(!school) return "";
    return school.displayName.length > 28 ? school.shortName : school.displayName;
}