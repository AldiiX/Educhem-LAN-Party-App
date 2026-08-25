export const platforms = [
    {id: "discord", name: "Discord", icon: "/icons/discord.svg", iconBackground: "#5865f2", disabled: false, avatarSyncPlatform: "Discord"},
    {id: "github", name: "GitHub", icon: "/icons/github.svg", iconBackground: "#f0f6fc", disabled: false, avatarSyncPlatform: "GitHub"},
    {id: "google", name: "Google", icon: "/icons/google.svg", iconBackground: "conic-gradient(from -45deg, #4285f4 0 25%, #34a853 25% 50%, #fbbc05 50% 75%, #ea4335 75% 100%)", disabled: false, avatarSyncPlatform: "Google"},
	{id: "apple", name: "Apple", icon: "/icons/apple.svg", iconBackground: "#000", disabled: true, avatarSyncPlatform: null},
	{id: "steam", name: "Steam", icon: "/icons/steam.svg", iconBackground: "#171a21", disabled: false, avatarSyncPlatform: "Steam"},
] as const satisfies readonly {
    id: string;
    name: string;
    icon: string;
    iconBackground: string;
    disabled: boolean;
    avatarSyncPlatform: string | null;
}[];

export type PlatformDefinition = (typeof platforms)[number];
export type ConnectablePlatformDefinition = Extract<PlatformDefinition, {readonly disabled: false}>;
export type ConnectablePlatform = ConnectablePlatformDefinition["id"];
export type AvatarSyncPlatformDefinition = Extract<ConnectablePlatformDefinition, {readonly avatarSyncPlatform: string}>;
export type AvatarSyncPlatform = AvatarSyncPlatformDefinition["avatarSyncPlatform"];

export const connectablePlatforms = platforms.filter(
    (platform): platform is ConnectablePlatformDefinition => !platform.disabled,
);

const avatarSyncPlatformDefinitions = connectablePlatforms.filter(
	(platform): platform is AvatarSyncPlatformDefinition => platform.avatarSyncPlatform != null,
);

export const avatarSyncPlatformValues = avatarSyncPlatformDefinitions.map(
    platform => platform.avatarSyncPlatform,
) as [AvatarSyncPlatform, ...AvatarSyncPlatform[]];

export const avatarSyncPlatforms = [
    {value: "", label: "Nesynchronizovat"},
    ...avatarSyncPlatformDefinitions.map(platform => ({value: platform.avatarSyncPlatform, label: platform.name})),
] as const;
