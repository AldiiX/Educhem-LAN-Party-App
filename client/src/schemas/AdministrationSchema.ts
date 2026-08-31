import {z} from "zod";
import {
    AccountGenderSchema,
    AccountSchema,
    AccountTypeSchema,
    SchoolSchema,
} from "@/schemas/AccountSchema";
import {LogEntrySchema} from "@/schemas/LogEntrySchema";

export const PaginationSchema = z.object({
    page: z.number(),
    pageSize: z.number(),
    totalEntries: z.number(),
    totalPages: z.number(),
});

const valueCount = <TValue extends z.ZodType>(value: TValue) => z.object({
    value,
    count: z.number(),
});

export const AdministrationAccountsPageSchema = z.object({
    accounts: z.array(AccountSchema),
    pagination: PaginationSchema,
    totalItems: z.number(),
    filterOptions: z.object({
        accountTypes: z.array(valueCount(AccountTypeSchema)),
        genders: z.array(valueCount(AccountGenderSchema)),
        classes: z.array(valueCount(z.string())),
        schools: z.array(z.object({
            school: SchoolSchema,
            count: z.number(),
        })),
    }),
});

export const AdministrationLogsPageSchema = z.object({
    logs: z.array(LogEntrySchema),
    pagination: PaginationSchema,
    totalItems: z.number(),
    filterOptions: z.object({
        logTypes: z.array(valueCount(z.string())),
        exactTypes: z.array(valueCount(z.string())),
    }),
});

export type Pagination = z.infer<typeof PaginationSchema>;
export type AdministrationAccountsPage = z.infer<typeof AdministrationAccountsPageSchema>;
export type AdministrationLogsPage = z.infer<typeof AdministrationLogsPageSchema>;
