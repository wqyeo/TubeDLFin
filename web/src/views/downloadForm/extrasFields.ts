import type { DownloadExtras } from '../../api/types';

/** Elements of the metadata checkboxes. */
export interface ExtrasElements {
    writeNfo: HTMLInputElement;
    saveThumbnail: HTMLInputElement;
    embedThumbnail: HTMLInputElement;
    embedMetadata: HTMLInputElement;
    embedChapters: HTMLInputElement;
}

/** What is saved besides the media file. */
export class ExtrasFields {
    constructor(private readonly elements: ExtrasElements) {}

    read(): DownloadExtras {
        return {
            WriteNfo: this.elements.writeNfo.checked,
            SaveThumbnail: this.elements.saveThumbnail.checked,
            EmbedThumbnail: this.elements.embedThumbnail.checked,
            EmbedMetadata: this.elements.embedMetadata.checked,
            EmbedChapters: this.elements.embedChapters.checked,
        };
    }

    write(extras: DownloadExtras): void {
        this.elements.writeNfo.checked = extras.WriteNfo;
        this.elements.saveThumbnail.checked = extras.SaveThumbnail;
        this.elements.embedThumbnail.checked = extras.EmbedThumbnail;
        this.elements.embedMetadata.checked = extras.EmbedMetadata;
        this.elements.embedChapters.checked = extras.EmbedChapters;
    }
}
