import type { ProbeRequest, ProbeResult, SubscriptionRequest } from '../api/types';
import { buildDownloadRequest, type DownloadFormValues } from '../downloads/downloadRequest';

/** The check intervals offered, in hours. The server accepts 1 to 168. */
export const intervalChoices: readonly number[] = [1, 3, 6, 12, 24, 48, 168];

/** The interval preselected, as on the server. */
export const defaultIntervalHours = 6;

/**
 * Turns the download form's values into a subscription to the detected channel or playlist.
 * Choices that only make sense for one download (a specific format, playlist items,
 * downloading again) are left out. Pure: no DOM.
 */
export function buildSubscriptionRequest(
    values: DownloadFormValues,
    probe: ProbeRequest,
    result: ProbeResult,
    name: string,
    intervalHours: number,
): SubscriptionRequest {
    const general = {
        ...values,
        videoQuality: values.videoQuality.startsWith('format:') ? 'best' : values.videoQuality,
        audioStream: 'best',
        playlistItems: '',
        redownload: false,
    };

    return {
        Url: probe.Url,
        Name: name.trim() || null,
        IntervalHours: intervalHours,
        Profile: buildDownloadRequest(general, probe, result).Profile,
    };
}

/** The name suggested for a subscription: the channel's for a channel, else the title. */
export function suggestedName(result: ProbeResult): string {
    return (result.Kind === 'Channel' ? result.Uploader : null) ?? result.Title;
}
