// Composition root of the dashboard page. jellyfin-web imports this module (from the page's
// data-controller attribute) and calls `new default(view, params)` once per page instance.
import { JellyfinHttpClient } from './api/httpClient';
import { JellyfinSettingsStore } from './api/settingsStore';
import { TubeDLFinApi } from './api/tubeDlFinApi';
import { injectStyles, requireElement } from './dom';
import { TubeDLFinPage } from './page';
import { pluginId } from './plugin';
import { DetectSection } from './sections/detectSection';
import { DownloadSection } from './sections/downloadSection';
import { InfoSection } from './sections/infoSection';
import { QueueSection } from './sections/queueSection';
import { SettingsSection } from './sections/settingsSection';
import { ToolsSection } from './sections/toolsSection';
import { DetectStore } from './state/detectStore';
import styles from './styles.css';
import { DetectForm } from './views/detectForm';
import { DetectResultView } from './views/detectResultView';
import { DownloadForm } from './views/downloadForm';
import { InfoPanel } from './views/infoPanel';
import { DashboardLoadingIndicator } from './views/loadingIndicator';
import { DashboardNotifier } from './views/notifier';
import { QueueView } from './views/queueView';
import { SettingsForm } from './views/settingsForm';
import { TabBar } from './views/tabs';
import { ToolsPanel } from './views/toolsPanel';

export default class TubeDLFinController {
    constructor(view: HTMLElement) {
        injectStyles(view, styles);
        const find = <T extends HTMLElement = HTMLElement>(name: string) =>
            requireElement<T>(view, `[data-tubedlfin="${name}"]`);

        new TabBar(view).select('download');
        const api = new TubeDLFinApi(new JellyfinHttpClient(ApiClient));

        // Download tab
        const detectStore = new DetectStore();
        const detectForm = new DetectForm({
            form: find<HTMLFormElement>('detect-form'),
            url: find<HTMLInputElement>('detect-url'),
            submit: find<HTMLButtonElement>('detect-submit'),
            cancel: find<HTMLButtonElement>('detect-cancel'),
            status: find('detect-status'),
            error: find('detect-error'),
        });
        const detectResult = new DetectResultView(find('detect-result'));
        detectStore.subscribe((state) => detectForm.render(state));
        detectStore.subscribe((state) => detectResult.render(state));
        const detect = new DetectSection(api, detectStore, [detectForm, detectResult], detectForm);
        const queue = new QueueSection(
            api,
            new QueueView({
                list: find('queue-list'),
                empty: find('queue-empty'),
                clearFinished: find<HTMLButtonElement>('queue-clear'),
                error: find('queue-error'),
            }),
        );
        const download = new DownloadSection(
            api,
            detectStore,
            new DownloadForm({
                form: find<HTMLFormElement>('download-form'),
                modeVideo: find<HTMLInputElement>('download-mode-video'),
                modeAudio: find<HTMLInputElement>('download-mode-audio'),
                videoOptions: find('download-video-options'),
                videoQuality: find<HTMLSelectElement>('download-video-quality'),
                container: find<HTMLSelectElement>('download-container'),
                h264Option: find('download-h264-option'),
                preferH264: find<HTMLInputElement>('download-prefer-h264'),
                audioOptions: find('download-audio-options'),
                audioStream: find<HTMLSelectElement>('download-audio-stream'),
                audioFormat: find<HTMLSelectElement>('download-audio-format'),
                audioQuality: find<HTMLSelectElement>('download-audio-quality'),
                playlistOptions: find('download-playlist-options'),
                playlistItems: find<HTMLInputElement>('download-playlist-items'),
                playlistHint: find('download-playlist-hint'),
                library: find<HTMLSelectElement>('download-library'),
                folderContainer: find('download-folder-container'),
                folder: find<HTMLSelectElement>('download-folder'),
                subfolder: find<HTMLInputElement>('download-subfolder'),
                redownload: find<HTMLInputElement>('download-redownload'),
                submit: find<HTMLButtonElement>('download-submit'),
                status: find('download-status'),
                error: find('download-error'),
            }),
            queue,
        );

        // Tools & settings tab
        const info = new InfoSection(api, new InfoPanel(find('info-list'), find('info-error')));
        const tools = new ToolsSection(
            api,
            new ToolsPanel({
                rows: find('tools-rows'),
                platform: find('tools-platform'),
                updateButton: find<HTMLButtonElement>('tools-update'),
                lastUpdate: find('tools-last-update'),
                error: find('tools-error'),
            }),
        );
        const settings = new SettingsSection(
            new JellyfinSettingsStore(ApiClient, pluginId),
            new SettingsForm({
                form: find<HTMLFormElement>('settings-form'),
                ytDlpPath: find<HTMLInputElement>('settings-ytdlp-path'),
                denoPath: find<HTMLInputElement>('settings-deno-path'),
                maxConcurrentDownloads: find<HTMLInputElement>('settings-max-concurrent'),
                error: find('settings-error'),
            }),
            new DashboardNotifier(Dashboard),
            () => tools.load(),
        );

        new TubeDLFinPage(
            view,
            [detect, download, queue, info, tools, settings],
            new DashboardLoadingIndicator(Dashboard),
        ).attach();
    }
}
