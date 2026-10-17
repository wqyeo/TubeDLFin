// Response shapes of the TubeDLFin API. Jellyfin serializes JSON in PascalCase.
// Keep in sync with the C# records in src/Jellyfin.Plugin.TubeDLFin.

/** GET /TubeDLFin/Info (C#: Status/TubeDLFinInfo.cs). */
export interface TubeDLFinInfo {
    PluginVersion: string;
    ServerVersion: string;
    OperatingSystem: string;
    Architecture: string;
    DataPath: string;
}
