"use client";

export type Announcement = {
    id: string;
    title: string;
    createdAt: Date;
};

export function useAnnouncements() {
    return {
        announcements: [] as Announcement[],
    };
}
