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
    MaxConcurrentDownloads: number;
    CookiesFile: string;
    ExtraArguments: string;
}

/** POST /TubeDLFin/Probe request body (C#: Api/ProbeRequest.cs). */
export interface ProbeRequest {
    Url: string;
    NoPlaylist: boolean;
}

/** C#: Detection/ProbeKind.cs */
export type ProbeKind = 'Video' | 'Playlist' | 'Channel';

/** C#: Detection/MediaFormat.cs */
export interface MediaFormat {
    FormatId: string;
    Extension: string;
    Note?: string | null;
    Width?: number | null;
    Height?: number | null;
    Fps?: number | null;
    VideoCodec?: string | null;
    AudioCodec?: string | null;
    Bitrate?: number | null;
    AudioBitrate?: number | null;
    FileSize?: number | null;
    IsFileSizeApproximate: boolean;
    DynamicRange?: string | null;
    Language?: string | null;
    Protocol?: string | null;
    HasVideo: boolean;
    HasAudio: boolean;
}

/** C#: Detection/SubtitleTrack.cs */
export interface SubtitleTrack {
    Code: string;
    Name?: string | null;
}

/** C#: Detection/PlaylistEntry.cs */
export interface PlaylistEntry {
    Index: number;
    Id?: string | null;
    Title: string;
    Url?: string | null;
    Duration?: number | null;
    Uploader?: string | null;
    IsCollection: boolean;
    ItemCount?: number | null;
    IsItemCountMinimum: boolean;
}

/** POST /TubeDLFin/Probe response (C#: Detection/ProbeResult.cs). */
export interface ProbeResult {
    Kind: ProbeKind;
    Id: string;
    Title: string;
    Url?: string | null;
    Site?: string | null;
    Uploader?: string | null;
    ChannelUrl?: string | null;
    Thumbnail?: string | null;
    Duration?: number | null;
    /** ISO date, e.g. "2005-04-24". */
    UploadDate?: string | null;
    Artist?: string | null;
    Album?: string | null;
    Track?: string | null;
    ReleaseYear?: number | null;
    VideoFormats: MediaFormat[];
    AudioFormats: MediaFormat[];
    Subtitles: SubtitleTrack[];
    AutomaticCaptions: SubtitleTrack[];
    Entries: PlaylistEntry[];
    ItemCount?: number | null;
    IsTruncated: boolean;
    HasPlaylistChoice: boolean;
    Warnings: string[];
}

/** C#: Libraries/LibraryType.cs */
export type LibraryType = 'Movies' | 'Shows' | 'Music' | 'MusicVideos' | 'HomeVideos' | 'Mixed';

/** C#: Downloads/Naming/NamingPresetId.cs */
export type NamingPresetId =
    | 'ChannelTitle'
    | 'MovieFolder'
    | 'TvShow'
    | 'Playlist'
    | 'MusicVideo'
    | 'Music'
    | 'Custom';

/** GET /TubeDLFin/NamingPresets (C#: Downloads/Naming/NamingPreset.cs). */
export interface NamingPreset {
    Id: NamingPresetId;
    Name: string;
    Example: string;
    MediaTemplate: string;
    ThumbnailTemplate: string;
    DefaultFor: LibraryType[];
}

/** C#: Api/LibraryFolderView.cs */
export interface LibraryFolder {
    Path: string;
    FreeBytes?: number | null;
}

/** GET /TubeDLFin/Libraries (C#: Api/LibraryView.cs). */
export interface LibraryInfo {
    Id: string;
    Name: string;
    Type: LibraryType;
    DefaultNaming: NamingPresetId;
    Folders: LibraryFolder[];
}

/** C#: Downloads/DownloadMode.cs */
export type DownloadMode = 'Video' | 'Audio';

/** C#: Downloads/VideoContainer.cs */
export type VideoContainer = 'Auto' | 'Mp4' | 'Mkv' | 'Webm';

/** C#: Downloads/AudioFormat.cs */
export type AudioFormat = 'Original' | 'M4a' | 'Mp3' | 'Opus' | 'Flac' | 'Wav';

/** C#: Downloads/DownloadExtras.cs */
export interface DownloadExtras {
    WriteNfo: boolean;
    SaveThumbnail: boolean;
    EmbedThumbnail: boolean;
    EmbedMetadata: boolean;
    EmbedChapters: boolean;
}

/** C#: Downloads/SubtitleChoice.cs */
export interface SubtitleChoice {
    Languages: string | null;
    IncludeAutomatic: boolean;
    Embed: boolean;
}

/** C#: Downloads/SponsorBlockMode.cs */
export type SponsorBlockMode = 'Off' | 'Remove' | 'Mark';

/** C#: Downloads/SponsorBlockCategory.cs */
export type SponsorBlockCategory =
    | 'Sponsor'
    | 'SelfPromo'
    | 'Interaction'
    | 'Intro'
    | 'Outro'
    | 'Preview'
    | 'Filler'
    | 'MusicOffTopic';

/** C#: Downloads/SponsorBlockChoice.cs */
export interface SponsorBlockChoice {
    Mode: SponsorBlockMode;
    Categories: SponsorBlockCategory[];
}

/** C#: Downloads/MusicTagging.cs */
export interface MusicTagging {
    Enabled: boolean;
    SplitArtistTitle: boolean;
    CleanTitles: boolean;
    NumberTracks: boolean;
    PlaylistAsAlbum: boolean;
    SquareCover: boolean;
}

/** C#: Downloads/DownloadProfile.cs */
export interface DownloadProfile {
    Mode: DownloadMode;
    MaxHeight: number | null;
    Container: VideoContainer;
    PreferH264: boolean;
    AudioFormat: AudioFormat;
    AudioQuality: number | null;
    LibraryId: string;
    LibraryFolder: string;
    Subfolder: string | null;
    Naming: NamingPresetId;
    CustomTemplate: string | null;
    Extras: DownloadExtras;
    Subtitles: SubtitleChoice;
    SponsorBlock: SponsorBlockChoice;
    Music: MusicTagging;
}

/** POST /TubeDLFin/Downloads request body (C#: Downloads/DownloadRequest.cs). */
export interface DownloadRequest {
    Url: string;
    NoPlaylist: boolean;
    FormatId: string | null;
    PlaylistItems: string | null;
    Redownload: boolean;
    Title: string | null;
    Profile: DownloadProfile;
}

/** C#: Downloads/DownloadState.cs */
export type DownloadState =
    | 'Queued'
    | 'Downloading'
    | 'Processing'
    | 'Completed'
    | 'CompletedWithErrors'
    | 'AlreadyDownloaded'
    | 'Failed'
    | 'Cancelled';

/** GET /TubeDLFin/Downloads (C#: Downloads/DownloadJobInfo.cs). */
export interface DownloadJobInfo {
    Id: string;
    Url: string;
    Title?: string | null;
    Source: string;
    LibraryName: string;
    Destination: string;
    State: DownloadState;
    Stage?: string | null;
    Percent?: number | null;
    DownloadedBytes?: number | null;
    TotalBytes?: number | null;
    Speed?: number | null;
    Eta?: number | null;
    ItemIndex?: number | null;
    ItemCount?: number | null;
    ItemTitle?: string | null;
    Files: string[];
    FailedItems: number;
    SkippedItems: number;
    Error?: string | null;
    CreatedAt: string;
    StartedAt?: string | null;
    FinishedAt?: string | null;
    IsFinished: boolean;
}

/** POST /TubeDLFin/Downloads/ClearFinished (C#: Api/ClearFinishedResult.cs). */
export interface ClearFinishedResult {
    Removed: number;
}
