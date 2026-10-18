// Composition root of the dashboard page. jellyfin-web imports this module (from the page's
// data-controller attribute) and calls `new default(view, params)` once per page instance.
import { JellyfinHttpClient } from './api/httpClient';
import { JellyfinSettingsStore } from './api/settingsStore';
import { TubeDLFinApi } from './api/tubeDlFinApi';
import { injectStyles, requireElement } from './dom';
import { BrowserOptionsMemory } from './downloads/optionsMemory';
import { TubeDLFinPage } from './page';
import { pluginId } from './plugin';
import { ActionSection } from './sections/actionSection';
import { DetectSection } from './sections/detectSection';
import { DownloadSection } from './sections/downloadSection';
import { InfoSection } from './sections/infoSection';
import { QueueSection } from './sections/queueSection';
import { SettingsSection } from './sections/settingsSection';
import { SubscribeSection } from './sections/subscribeSection';
import { SubscriptionsSection } from './sections/subscriptionsSection';
import { ToolsSection } from './sections/toolsSection';
import { DetectStore } from './state/detectStore';
import styles from './styles.css';
import { SubscriptionDirectory } from './subscriptions/subscriptionDirectory';
import { ActionChoice } from './views/actionChoice';
import { DetectForm } from './views/detectForm';
import { DetectResultView } from './views/detectResultView';
import { DestinationFields } from './views/downloadForm/destinationFields';
import { DownloadForm } from './views/downloadForm/downloadForm';
import { ExtrasFields } from './views/downloadForm/extrasFields';
import { MusicFields } from './views/downloadForm/musicFields';
import { NamingFields } from './views/downloadForm/namingFields';
import { SponsorBlockFields } from './views/downloadForm/sponsorBlockFields';
import { SubtitleFields } from './views/downloadForm/subtitleFields';
import { InfoPanel } from './views/infoPanel';
import { DashboardLoadingIndicator } from './views/loadingIndicator';
import { DashboardNotifier } from './views/notifier';
import { QueueView } from './views/queueView';
import { SettingsForm } from './views/settingsForm';
import { SubscribeForm } from './views/subscribeForm';
import { SubscriptionsView } from './views/subscriptionsView';
import { TabBar } from './views/tabs';
import { ToolsPanel } from './views/toolsPanel';

export default class TubeDLFinController {
    constructor(view: HTMLElement) {
        injectStyles(view, styles);
        const find = <T extends HTMLElement = HTMLElement>(name: string) =>
            requireElement<T>(view, `[data-tubedlfin="${name}"]`);

        const tabs = new TabBar(view);
        tabs.select('download');
        const api = new TubeDLFinApi(new JellyfinHttpClient(ApiClient));
        const subscriptionNames = new SubscriptionDirectory();
        const optionsMemory = new BrowserOptionsMemory(window.localStorage);

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
        const queueView = new QueueView(
            {
                list: find('queue-list'),
                empty: find('queue-empty'),
                clearFinished: find<HTMLButtonElement>('queue-clear'),
                error: find('queue-error'),
            },
            subscriptionNames,
        );
        subscriptionNames.subscribe(() => queueView.redraw());
        const queue = new QueueSection(api, queueView);
        const downloadForm = new DownloadForm(
            {
                form: find<HTMLFormElement>('download-form'),
                title: find('download-title'),
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
                redownloadOption: find('download-redownload-option'),
                redownload: find<HTMLInputElement>('download-redownload'),
                submit: find<HTMLButtonElement>('download-submit'),
                status: find('download-status'),
                error: find('download-error'),
            },
            {
                destination: new DestinationFields({
                    library: find<HTMLSelectElement>('download-library'),
                    folderContainer: find('download-folder-container'),
                    folder: find<HTMLSelectElement>('download-folder'),
                    subfolder: find<HTMLInputElement>('download-subfolder'),
                    freeSpace: find('download-free-space'),
                }),
                naming: new NamingFields({
                    preset: find<HTMLSelectElement>('download-naming'),
                    customContainer: find('download-custom-template-container'),
                    custom: find<HTMLInputElement>('download-custom-template'),
                    example: find('download-naming-example'),
                }),
                extras: new ExtrasFields({
                    writeNfo: find<HTMLInputElement>('download-write-nfo'),
                    saveThumbnail: find<HTMLInputElement>('download-save-thumbnail'),
                    embedThumbnail: find<HTMLInputElement>('download-embed-thumbnail'),
                    embedMetadata: find<HTMLInputElement>('download-embed-metadata'),
                    embedChapters: find<HTMLInputElement>('download-embed-chapters'),
                }),
                subtitles: new SubtitleFields({
                    chips: find('download-subtitle-chips'),
                    languages: find<HTMLInputElement>('download-subtitle-languages'),
                    includeAutomatic: find<HTMLInputElement>('download-subtitle-auto'),
                    embed: find<HTMLInputElement>('download-subtitle-embed'),
                }),
                music: new MusicFields({
                    container: find('download-music-options'),
                    split: find<HTMLInputElement>('download-music-split'),
                    clean: find<HTMLInputElement>('download-music-clean'),
                    numberOption: find('download-music-number-option'),
                    number: find<HTMLInputElement>('download-music-number'),
                    albumOption: find('download-music-album-option'),
                    album: find<HTMLInputElement>('download-music-album'),
                    square: find<HTMLInputElement>('download-music-square'),
                }),
                sponsorBlock: new SponsorBlockFields({
                    mode: find<HTMLSelectElement>('download-sponsorblock-mode'),
                    categories: find('download-sponsorblock-categories'),
                }),
            },
        );
        const download = new DownloadSection(api, detectStore, downloadForm, optionsMemory, queue);

        // Subscriptions tab, and the "Download now / Subscribe" choice in the download form
        const subscriptions = new SubscriptionsSection(
            api,
            new SubscriptionsView({
                list: find('subscriptions-list'),
                empty: find('subscriptions-empty'),
                error: find('subscriptions-error'),
            }),
            subscriptionNames,
            queue,
            (message) => window.confirm(message),
        );
        const subscribe = new SubscribeSection(
            api,
            detectStore,
            downloadForm,
            optionsMemory,
            queue,
            subscriptions,
        );
        const action = new ActionSection(
            detectStore,
            new ActionChoice({
                container: find('action'),
                download: find<HTMLInputElement>('action-download'),
                subscribe: find<HTMLInputElement>('action-subscribe'),
            }),
            new SubscribeForm({
                container: find('subscribe'),
                name: find<HTMLInputElement>('subscribe-name'),
                interval: find<HTMLSelectElement>('subscribe-interval'),
                includeExisting: find<HTMLInputElement>('subscribe-existing'),
            }),
            downloadForm,
            download,
            subscribe,
        );
        tabs.onSelect((name) => {
            if (name === 'subscriptions') {
                void subscriptions.refresh();
            }
        });

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
                cookiesFile: find<HTMLInputElement>('settings-cookies-file'),
                extraArguments: find<HTMLInputElement>('settings-extra-arguments'),
                error: find('settings-error'),
            }),
            new DashboardNotifier(Dashboard),
            () => tools.load(),
        );

        new TubeDLFinPage(
            view,
            [detect, download, action, queue, subscriptions, info, tools, settings],
            new DashboardLoadingIndicator(Dashboard),
        ).attach();
    }
}
