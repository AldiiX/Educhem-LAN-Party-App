import {z} from "zod";
import {apiFetch} from "@/lib/apiClient";
import {AccountCommunicationStyleSchema} from "@/schemas/AccountSchema";

const EmailChangeStatusSchema = z.object({
    id: z.string(),
    oldEmail: z.string(),
    newEmail: z.string(),
    expiresAtUtc: z.string(),
    oldConfirmed: z.boolean(),
    newConfirmed: z.boolean(),
    state: z.enum(["pending", "completed", "cancelled", "expired"]),
    resendAtUtc: z.string().nullable(),
    communicationStyle: AccountCommunicationStyleSchema,
});

const EmailChangeResponseSchema = z.object({
    request: EmailChangeStatusSchema.nullable(),
    emailsSent: z.boolean(),
    tokenAction: z.enum(["old", "new", "cancel"]).nullable(),
});

export type EmailChangeStatus = z.infer<typeof EmailChangeStatusSchema>;

export async function emailChangeRequest(path = "", body?: object) {
    const response = await apiFetch(`/api/v1/account/email-change${path}`, {
        method: body === undefined ? "GET" : "POST",
        cache: "no-store",
        ...(body === undefined ? {} : {headers: {"Content-Type": "application/json"}, body: JSON.stringify(body)}),
    });
    if (response.status === 401) throw new Error("Přihlášení už neplatí. Přihlaste se znovu.");
    if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message ?? (response.status === 429
            ? "Příliš mnoho pokusů. Zkuste to později."
            : "Požadavek se nepodařilo dokončit. Obnovte stránku a zkuste to znovu."));
    }
    return EmailChangeResponseSchema.parse(await response.json());
}

export function emailChangeDate(value: string) {
    return new Intl.DateTimeFormat("cs-CZ", {
        dateStyle: "short", timeStyle: "short", timeZone: "Europe/Prague",
    }).format(new Date(value));
}
