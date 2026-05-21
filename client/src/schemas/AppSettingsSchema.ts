export type ReservationStatusType = "UseTimer" | "Open" | "Closed";

export type AppSettings = {
    chatEnabled: boolean;
    serverNow: Date;
    reservationsEnabledFrom: Date;
    reservationsEnabledTo: Date;
    reservationsStatus: ReservationStatusType;
    reservationsEnabledRightNow: boolean;
};

export type AppSettingsResponse = {
    chatEnabled: boolean;
    serverNow: string;
    reservationsEnabledFrom: string;
    reservationsEnabledTo: string;
    reservationsStatus: ReservationStatusType;
    reservationsEnabledRightNow: boolean;
};

export function mapAppSettings(data: AppSettingsResponse): AppSettings {
    return {
        chatEnabled: data.chatEnabled,
        serverNow: new Date(data.serverNow),
        reservationsEnabledFrom: new Date(data.reservationsEnabledFrom),
        reservationsEnabledTo: new Date(data.reservationsEnabledTo),
        reservationsStatus: data.reservationsStatus,
        reservationsEnabledRightNow: data.reservationsEnabledRightNow,
    };
}