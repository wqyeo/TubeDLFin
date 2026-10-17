// Response shapes of the TubeDLFin API. Jellyfin serializes JSON in PascalCase, enums as strings,
// and leaves out properties that are null, so nullable C# properties are optional here.
// Keep in sync with the C# records in src/Jellyfin.Plugin.TubeDLFin.

/** GET /TubeDLFin/Info (C#: Status/TubeDLFinInfo.cs). */
export interface TubeDLFinInfo {
    PluginVersion: string;
    ServerVersion: string;
    OperatingSystem: string;
    Architecture: string;
    DataPath: string;
}

/** C#: Tools/ToolKind.cs */
export type ToolKind = 'YtDlp' | 'Deno';

/** C#: Tools/Status/ToolState.cs */
export type ToolState = 'Ready' | 'NotInstalled' | 'Broken' | 'Unsupported';

/** C#: Tools/Updates/ToolUpdateOutcome.cs */
export type ToolUpdateOutcome =
    | 'Installed'
    | 'Updated'
    | 'UpToDate'
    | 'SkippedCustom'
    | 'Unsupported'
    | 'Failed';

/** C#: Tools/Status/ToolStatus.cs */
export interface ToolStatus {
    Kind: ToolKind;
    Name: string;
    State: ToolState;
    Version?: string | null;
    Path: string;
    IsCustom: boolean;
    Message?: string | null;
}

/** C#: Tools/Status/FfmpegStatus.cs */
export interface FfmpegStatus {
    EncoderPath?: string | null;
    ProbePath?: string | null;
    Version?: string | null;
    Found: boolean;
}

/** C#: Tools/Updates/ToolUpdateResult.cs */
export interface ToolUpdateResult {
    Kind: ToolKind;
    Name: string;
    Outcome: ToolUpdateOutcome;
    Version?: string | null;
    Message?: string | null;
}

/** C#: Tools/Updates/ToolUpdateReport.cs */
export interface ToolUpdateReport {
    FinishedAt: string;
    Results: ToolUpdateResult[];
}

/** GET /TubeDLFin/Tools and POST /TubeDLFin/Tools/Update (C#: Tools/Status/ToolsOverview.cs). */
export interface ToolsOverview {
    Platform: string;
    Tools: ToolStatus[];
    Ffmpeg: FfmpegStatus;
    IsUpdating: boolean;
    LastUpdate?: ToolUpdateReport | null;
}

/** The settings TubeDLFin edits (C#: Configuration/PluginConfiguration.cs). */
export interface TubeDLFinSettings {
    YtDlpPath: string;
    DenoPath: string;
}
