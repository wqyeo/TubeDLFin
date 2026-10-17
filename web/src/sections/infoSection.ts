import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import { describeError } from '../errors';
import type { InfoPanel } from '../views/infoPanel';
import type { PageSection } from './pageSection';

/** The "Status" section: plugin and server facts. */
export class InfoSection implements PageSection {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly panel: InfoPanel,
    ) {}

    async load(): Promise<void> {
        try {
            this.panel.showInfo(await this.api.getInfo());
        } catch (error) {
            this.panel.showError(describeError(error));
        }
    }
}
