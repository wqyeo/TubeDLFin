import type { SettingsStore } from '../api/settingsStore';
import type { TubeDLFinSettings } from '../api/types';
import { describeError } from '../errors';
import type { Notifier } from '../views/notifier';
import type { SettingsForm } from '../views/settingsForm';
import type { PageSection } from './pageSection';

/** The "Settings" section. Calls `onSaved` so other sections can refresh. */
export class SettingsSection implements PageSection {
    constructor(
        private readonly store: SettingsStore,
        private readonly form: SettingsForm,
        private readonly notifier: Notifier,
        private readonly onSaved: () => Promise<void>,
    ) {
        form.onSubmit((settings) => {
            void this.save(settings);
        });
    }

    async load(): Promise<void> {
        try {
            this.form.show(await this.store.load());
        } catch (error) {
            this.form.showError(describeError(error));
        }
    }

    private async save(settings: TubeDLFinSettings): Promise<void> {
        try {
            await this.store.save(settings);
            this.notifier.settingsSaved();
            await this.onSaved();
        } catch (error) {
            this.form.showError(describeError(error));
        }
    }
}
