export const platforms = [
    {id: "discord", name: "Discord", icon: "/icons/discord.svg", iconBackground: "#5865f2", disabled: false, avatarSyncPlatform: "Discord"},
    {id: "github", name: "GitHub", icon: "/icons/github.svg", iconBackground: "#f0f6fc", disabled: false, avatarSyncPlatform: "GitHub"},
    {id: "google", name: "Google", icon: "/icons/google.svg", iconBackground: "conic-gradient(from -45deg, #4285f4 0 25%, #34a853 25% 50%, #fbbc05 50% 75%, #ea4335 75% 100%)", disabled: false, avatarSyncPlatform: "Google"},
	{id: "steam", name: "Steam", icon: "/icons/steam.svg", iconBackground: "#171a21", disabled: false, avatarSyncPlatform: "Steam"},
    {id: "instagram", name: "Instagram", icon: "/icons/instagram.svg", iconBackground: "radial-gradient(circle at 30% 107%, #fdf497 0 5%, #fd5949 45%, #d6249f 60%, #285aeb 90%)", disabled: true, avatarSyncPlatform: "Instagram"},
] as const satisfies readonly {
    id: string;
    name: string;
    icon: string;
    iconBackground: string;
    disabled: boolean;
    avatarSyncPlatform: string;
}[];

export type PlatformDefinition = (typeof platforms)[number];
export type ConnectablePlatformDefinition = Extract<PlatformDefinition, {readonly disabled: false}>;
export type ConnectablePlatform = ConnectablePlatformDefinition["id"];
export type AvatarSyncPlatform = ConnectablePlatformDefinition["avatarSyncPlatform"];

export const connectablePlatforms = platforms.filter(
    (platform): platform is ConnectablePlatformDefinition => !platform.disabled,
);

export const avatarSyncPlatformValues = connectablePlatforms.map(
    platform => platform.avatarSyncPlatform,
) as [AvatarSyncPlatform, ...AvatarSyncPlatform[]];

export const avatarSyncPlatforms = [
    {value: "", label: "Nesynchronizovat"},
    ...connectablePlatforms.map(platform => ({value: platform.avatarSyncPlatform, label: platform.name})),
] as const;
