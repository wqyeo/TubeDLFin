import type { TubeDLFinApi } from '../api/tubeDlFinApi';
import type { LibraryInfo, ProbeResult } from '../api/types';
import { buildDownloadRequest, type DownloadFormValues } from '../downloads/downloadRequest';
import { categoryOf, libraryDefaults } from '../downloads/libraryDefaults';
import type { OptionsMemory } from '../downloads/optionsMemory';
import { describeError } from '../errors';
import type { DetectState, DetectStore } from '../state/detectStore';
import type { DownloadForm } from '../views/downloadForm/downloadForm';
import type { PageSection } from './pageSection';

/** Something that shows the queue and can be told to refresh it. */
export interface QueueRefresher {
    refresh(): Promise<void>;
}

/**
 * The "Download" step: shows the options form for the detected link, fills it from the
 * library's defaults and the options remembered for music or video, and queues the download
 * when the form is submitted to download (see ActionSection).
 */
export class DownloadSection implements PageSection {
    constructor(
        private readonly api: TubeDLFinApi,
        private readonly detectStore: DetectStore,
        private readonly form: DownloadForm,
        private readonly memory: OptionsMemory,
        private readonly queue: QueueRefresher,
    ) {
        detectStore.subscribe((state) => this.onDetectChanged(state));
        form.onLibraryChanged((library) => this.applyLibrary(library));
    }

    async load(): Promise<void> {
        try {
            const [libraries, presets] = await Promise.all([
                this.api.getLibraries(),
                this.api.getNamingPresets(),
            ]);
            this.form.setNamingPresets(presets);
            this.form.setLibraries(libraries);
        } catch (error) {
            this.form.showError(describeError(error));
        }
    }

    private onDetectChanged(state: DetectState): void {
        if (state.status !== 'detected') {
            this.form.hide();
            return;
        }

        this.form.show(state.result);
        const library = this.form.selectLibrary(this.memory.lastLibraryId()) ?? this.form.selectedLibrary();
        if (library) {
            this.applyLibrary(library, state.result);
        }
    }

    // Library defaults, overlaid with what was last used for music or video. Naming and the
    // folder are only taken over from the same library: a TV-show layout doesn't suit a movie
    // library just because both are "video".
    private applyLibrary(library: LibraryInfo, result?: ProbeResult): void {
        const defaults = libraryDefaults(library);
        const remembered = this.memory.load(categoryOf(library)) ?? {};
        const sameLibrary = remembered.libraryId === library.Id;
        const {
            libraryId: _libraryId,
            libraryFolder,
            subfolder,
            naming,
            customTemplate,
            ...general
        } = remembered;

        this.form.apply({
            ...defaults,
            ...general,
            ...(sameLibrary && naming ? { naming, customTemplate: customTemplate ?? '' } : {}),
            ...(sameLibrary && libraryFolder ? { libraryFolder } : {}),
            subfolder: sameLibrary ? (subfolder ?? '') : '',
        });

        // Sites without video (SoundCloud, ...) always start in audio mode.
        const detected =
            result ??
            (this.detectStore.current.status === 'detected' ? this.detectStore.current.result : undefined);
        if (detected && detected.VideoFormats.length === 0 && detected.AudioFormats.length > 0) {
            this.form.apply({ mode: 'Audio' });
        }
    }

    async submit(values: DownloadFormValues): Promise<void> {
        const state = this.detectStore.current;
        if (state.status !== 'detected') {
            return;
        }

        this.form.setBusy(true);
        try {
            const job = await this.api.addDownload(buildDownloadRequest(values, state.request, state.result));
            const library = this.form.selectedLibrary();
            if (library) {
                this.memory.save(categoryOf(library), values);
            }

            this.form.showDone(`Added "${job.Title ?? state.result.Title}" to the queue.`);
            await this.queue.refresh();
        } catch (error) {
            this.form.showError(describeError(error));
        } finally {
            this.form.setBusy(false);
        }
    }
}
