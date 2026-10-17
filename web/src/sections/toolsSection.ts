import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import { describeError } from '../errors';
import type { ToolsPanel } from '../views/toolsPanel';
import type { PageSection } from './pageSection';

/** The "Tools" section: yt-dlp, Deno and FFmpeg status, and "Update now". */
export class ToolsSection implements PageSection {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly panel: ToolsPanel,
    ) {
        panel.onUpdateClicked(() => {
            void this.update();
        });
    }

    async load(): Promise<void> {
        try {
            this.panel.show(await this.api.getTools());
        } catch (error) {
            this.panel.showError(describeError(error));
        }
    }

    private async update(): Promise<void> {
        this.panel.setBusy(true);
        try {
            this.panel.show(await this.api.updateTools());
        } catch (error) {
            this.panel.setBusy(false);
            this.panel.showError(describeError(error));
        }
    }
}
