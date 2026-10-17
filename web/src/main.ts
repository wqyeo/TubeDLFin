// Composition root of the dashboard page. jellyfin-web imports this module (from the page's
// data-controller attribute) and calls `new default(view, params)` once per page instance.
import { JellyfinHttpClient } from './api/httpClient';
import { JellyfinSettingsStore } from './api/settingsStore';
import { TubeDLFinApi } from './api/tubeDlFinApi';
import { injectStyles, requireElement } from './dom';
import { TubeDLFinPage } from './page';
import { pluginId } from './plugin';
import { InfoSection } from './sections/infoSection';
import { SettingsSection } from './sections/settingsSection';
import { ToolsSection } from './sections/toolsSection';
import styles from './styles.css';
import { InfoPanel } from './views/infoPanel';
import { DashboardLoadingIndicator } from './views/loadingIndicator';
import { DashboardNotifier } from './views/notifier';
import { SettingsForm } from './views/settingsForm';
import { ToolsPanel } from './views/toolsPanel';

export default class TubeDLFinController {
    constructor(view: HTMLElement) {
        injectStyles(view, styles);
        const find = <T extends HTMLElement = HTMLElement>(name: string) =>
            requireElement<T>(view, `[data-tubedlfin="${name}"]`);

        const api = new TubeDLFinApi(new JellyfinHttpClient(ApiClient));

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
                error: find('settings-error'),
            }),
            new DashboardNotifier(Dashboard),
            () => tools.load(),
        );

        new TubeDLFinPage(view, [info, tools, settings], new DashboardLoadingIndicator(Dashboard)).attach();
    }
}
