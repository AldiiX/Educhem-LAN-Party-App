"use client";

import {useMemo, useState, type FormEvent} from "react";
import useSWR from "swr";
import {fetcher} from "@/lib/swr";
import type {Account} from "@/schemas/AccountSchema";

export type ProblemReportCategory =
    "TechnicalProblem" |
    "PhysicalDevicePc" |
    "PhysicalDevicePeripheral" |
    "Network" |
    "Reservation" |
    "Account" |
    "ApplicationBug" |
    "Tournament" |
    "Facility" |
    "Safety" |
    "Other";

export type ProblemReportPriority = "Low" | "Medium" | "High" | "Critical";
export type ProblemReportStatus = "Pending" | "Resolved" | "Unresolved";

export type ProblemReportItem = {
    id: string;
    category: ProblemReportCategory;
    priority: ProblemReportPriority;
    status: ProblemReportStatus;
    title: string;
    description: string;
    contact: string | null;
    resolutionNote: string | null;
    createdAtUtc: Date;
    updatedAtUtc: Date;
    resolvedAtUtc: Date | null;
    reporter: Account;
    resolvedBy: Account | null;
};

type ProblemReportForm = {
    category: ProblemReportCategory;
    priority: ProblemReportPriority;
    title: string;
    description: string;
    contact: string;
};

type ResolveDraft = {
    status: ProblemReportStatus;
    resolutionNote: string;
};

type AccountResponse = Omit<Account, "createdAtUtc" | "updatedAtUtc" | "lastActiveUtc"> & {
    createdAtUtc: string;
    updatedAtUtc?: string | null;
    lastActiveUtc?: string | null;
};

type ProblemReportResponse = Omit<ProblemReportItem, "createdAtUtc" | "updatedAtUtc" | "resolvedAtUtc" | "reporter" | "resolvedBy"> & {
    createdAtUtc: string;
    updatedAtUtc: string;
    resolvedAtUtc: string | null;
    reporter: AccountResponse;
    resolvedBy: AccountResponse | null;
};

const initialForm: ProblemReportForm = {
    category: "TechnicalProblem",
    priority: "Medium",
    title: "",
    description: "",
    contact: "",
};

const categoryOptions: {value: ProblemReportCategory; label: string}[] = [
    {value: "TechnicalProblem", label: "Technický problém"},
    {value: "PhysicalDevicePc", label: "Porucha fyzického zařízení (PC)"},
    {value: "PhysicalDevicePeripheral", label: "Porucha periferie"},
    {value: "Network", label: "Síť / internet"},
    {value: "Reservation", label: "Rezervace"},
    {value: "Account", label: "Účet / přihlášení"},
    {value: "ApplicationBug", label: "Bug v aplikaci"},
    {value: "Tournament", label: "Turnaje"},
    {value: "Facility", label: "Místnost / vybavení"},
    {value: "Safety", label: "Bezpečnost"},
    {value: "Other", label: "Ostatní"},
];

const priorityOptions: {value: ProblemReportPriority; label: string}[] = [
    {value: "Low", label: "Nízká"},
    {value: "Medium", label: "Střední"},
    {value: "High", label: "Vysoká"},
    {value: "Critical", label: "Kritická"},
];

const statusOptions: {value: ProblemReportStatus; label: string}[] = [
    {value: "Pending", label: "Čeká na vyřešení"},
    {value: "Resolved", label: "Vyřešeno"},
    {value: "Unresolved", label: "Neopraveno"},
];

export const categoryLabels = Object.fromEntries(categoryOptions.map(option => [option.value, option.label])) as Record<ProblemReportCategory, string>;
export const priorityLabels = Object.fromEntries(priorityOptions.map(option => [option.value, option.label])) as Record<ProblemReportPriority, string>;
export const statusLabels = Object.fromEntries(statusOptions.map(option => [option.value, option.label])) as Record<ProblemReportStatus, string>;

function parseAccount(account: AccountResponse): Account {
    return {
        ...account,
        createdAtUtc: new Date(account.createdAtUtc),
        updatedAtUtc: account.updatedAtUtc ? new Date(account.updatedAtUtc) : null,
        lastActiveUtc: account.lastActiveUtc ? new Date(account.lastActiveUtc) : null,
    };
}

function parseReport(item: ProblemReportResponse): ProblemReportItem {
    return {
        ...item,
        createdAtUtc: new Date(item.createdAtUtc),
        updatedAtUtc: new Date(item.updatedAtUtc),
        resolvedAtUtc: item.resolvedAtUtc ? new Date(item.resolvedAtUtc) : null,
        reporter: parseAccount(item.reporter),
        resolvedBy: item.resolvedBy ? parseAccount(item.resolvedBy) : null,
    };
}

const reportsFetcher = async (url: string) => {
    const reports = await fetcher<ProblemReportResponse[]>(url);
    return reports.map(parseReport);
};

export function useProblemReport() {
    const {data, error, isLoading, mutate} = useSWR<ProblemReportItem[]>("/api/v1/problem-reports", reportsFetcher);
    const [form, setForm] = useState<ProblemReportForm>(initialForm);
    const [wasSubmitted, setWasSubmitted] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [pendingReportId, setPendingReportId] = useState<string | null>(null);
    const [resolveDrafts, setResolveDrafts] = useState<Record<string, ResolveDraft>>({});
    const [search, setSearch] = useState("");
    const [statusFilter, setStatusFilter] = useState<ProblemReportStatus | "all">("all");
    const [priorityFilter, setPriorityFilter] = useState<ProblemReportPriority | "all">("all");

    const canSubmit = useMemo(() => {
        return form.title.trim().length > 0 && form.description.trim().length > 0;
    }, [form.description, form.title]);

    const filteredReports = useMemo(() => {
        const query = search.trim().toLowerCase();

        return (data ?? []).filter(item => {
            if(statusFilter !== "all" && item.status !== statusFilter) return false;
            if(priorityFilter !== "all" && item.priority !== priorityFilter) return false;
            if(query.length === 0) return true;

            return [
                item.title,
                item.description,
                item.contact ?? "",
                item.reporter.fullName,
                item.resolvedBy?.fullName ?? "",
                categoryLabels[item.category],
                priorityLabels[item.priority],
                statusLabels[item.status],
            ].some(value => value.toLowerCase().includes(query));
        });
    }, [data, priorityFilter, search, statusFilter]);

    const clearFilters = () => {
        setSearch("");
        setStatusFilter("all");
        setPriorityFilter("all");
    };

    const updateField = (field: keyof ProblemReportForm, value: string) => {
        setWasSubmitted(false);
        setSubmitError(null);
        setForm(currentForm => ({
            ...currentForm,
            [field]: value,
        }));
    };

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if(!canSubmit || isSubmitting) return;

        setIsSubmitting(true);
        setSubmitError(null);
        setWasSubmitted(false);

        try {
            const response = await fetch("/api/v1/problem-reports", {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify(form),
            });

            if(!response.ok) throw new Error("Problem report request failed.");

            setForm(initialForm);
            setWasSubmitted(true);
            setIsCreateModalOpen(false);
            await mutate();
        } catch {
            setSubmitError("Hlášení se nepodařilo odeslat.");
        } finally {
            setIsSubmitting(false);
        }
    };

    const updateResolveDraft = (id: string, field: keyof ResolveDraft, value: ResolveDraft[keyof ResolveDraft]) => {
        const currentReport = data?.find(item => item.id === id);
        setResolveDrafts(currentDrafts => ({
            ...currentDrafts,
            [id]: {
                ...{
                    status: currentReport?.status ?? "Pending",
                    resolutionNote: currentReport?.resolutionNote ?? "",
                },
                ...currentDrafts[id],
                [field]: value,
            },
        }));
    };

    const getResolveDraft = (id: string) => {
        const currentReport = data?.find(item => item.id === id);

        return {
            ...{
                status: currentReport?.status ?? "Pending",
                resolutionNote: currentReport?.resolutionNote ?? "",
            },
            ...resolveDrafts[id],
        };
    };

    const resolveReport = async (id: string) => {
        const draft = getResolveDraft(id);

        setPendingReportId(id);
        try {
            const response = await fetch(`/api/v1/problem-reports/${id}/status`, {
                method: "PUT",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify(draft),
            });

            if(!response.ok) throw new Error("Problem report status request failed.");
            await mutate();
        } finally {
            setPendingReportId(null);
        }
    };

    const deleteReport = async (id: string) => {
        setPendingReportId(id);
        try {
            const response = await fetch(`/api/v1/problem-reports/${id}`, {method: "DELETE"});

            if(!response.ok) throw new Error("Problem report delete request failed.");
            await mutate();
        } finally {
            setPendingReportId(null);
        }
    };

    const openCreateModal = () => {
        setWasSubmitted(false);
        setSubmitError(null);
        setIsCreateModalOpen(true);
    };

    const closeCreateModal = () => {
        if(isSubmitting) return;
        setSubmitError(null);
        setIsCreateModalOpen(false);
    };

    return {
        form,
        reports: data ?? [],
        filteredReports,
        reportsError: error,
        isLoadingReports: isLoading,
        canSubmit,
        wasSubmitted,
        submitError,
        isSubmitting,
        isCreateModalOpen,
        pendingReportId,
        resolveDrafts,
        categoryOptions,
        priorityOptions,
        statusOptions,
        search,
        statusFilter,
        priorityFilter,
        categoryLabels,
        priorityLabels,
        statusLabels,
        setSearch,
        setStatusFilter,
        setPriorityFilter,
        clearFilters,
        updateField,
        submit,
        openCreateModal,
        closeCreateModal,
        updateResolveDraft,
        resolveReport,
        deleteReport,
        getResolveDraft,
    };
}

export type ProblemReportHook = ReturnType<typeof useProblemReport>;
