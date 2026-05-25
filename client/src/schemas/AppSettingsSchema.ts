export type ReservationStatusType = "UseTimer" | "Open" | "Closed";

export type AppSettingsItem = {
    chatEnabled: boolean;
    serverNow: Date;
    reservationsEnabledFrom: Date;
    reservationsEnabledTo: Date;
    reservationsStatus: ReservationStatusType;
    reservationsEnabledRightNow: boolean;
    attendanceEnabled: boolean;
    problemReportsEnabled: boolean;
};

export type AppSettingsResponse = {
    chatEnabled: boolean;
    serverNow: string;
    reservationsEnabledFrom: string;
    reservationsEnabledTo: string;
    reservationsStatus: ReservationStatusType;
    reservationsEnabledRightNow: boolean;
    attendanceEnabled: boolean;
    problemReportsEnabled: boolean;
};

export function mapAppSettings(data: AppSettingsResponse): AppSettingsItem {
    return {
        chatEnabled: data.chatEnabled,
        serverNow: new Date(data.serverNow),
        reservationsEnabledFrom: new Date(data.reservationsEnabledFrom),
        reservationsEnabledTo: new Date(data.reservationsEnabledTo),
        reservationsStatus: data.reservationsStatus,
        reservationsEnabledRightNow: data.reservationsEnabledRightNow,
        attendanceEnabled: data.attendanceEnabled,
        problemReportsEnabled: data.problemReportsEnabled,
    };
}
