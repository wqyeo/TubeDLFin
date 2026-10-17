// Composition root of the dashboard page. jellyfin-web imports this module (from the page's
// data-controller attribute) and calls `new default(view, params)` once per page instance.
import { JellyfinHttpClient } from './api/httpClient';
import { TubeDLFinApi } from './api/tubeDlFinApi';
import { injectStyles, requireElement } from './dom';
import { TubeDLFinPage } from './page';
import styles from './styles.css';
import { InfoPanel } from './views/infoPanel';
import { DashboardLoadingIndicator } from './views/loadingIndicator';

export default class TubeDLFinController {
    constructor(view: HTMLElement) {
        injectStyles(view, styles);

        const api = new TubeDLFinApi(new JellyfinHttpClient(ApiClient));
        const infoPanel = new InfoPanel(
            requireElement(view, '[data-tubedlfin="info-list"]'),
            requireElement(view, '[data-tubedlfin="info-error"]'),
        );

        new TubeDLFinPage(view, api, infoPanel, new DashboardLoadingIndicator(Dashboard)).attach();
    }
}
