import type { MusicTagging } from '../../api/types';

/** Elements of the music tag options. */
export interface MusicElements {
    container: HTMLElement;
    split: HTMLInputElement;
    clean: HTMLInputElement;
    numberOption: HTMLElement;
    number: HTMLInputElement;
    albumOption: HTMLElement;
    album: HTMLInputElement;
    square: HTMLInputElement;
}

/** The music tagging choices without "Enabled", which the form decides. */
export type MusicChoices = Omit<MusicTagging, 'Enabled'>;

/** Music tag options: shown for audio downloads and music video libraries. */
export class MusicFields {
    constructor(private readonly elements: MusicElements) {}

    /** Shows the group; the playlist options only for lists. */
    show(visible: boolean, isList: boolean): void {
        this.elements.container.hidden = !visible;
        this.elements.numberOption.hidden = !isList;
        this.elements.albumOption.hidden = !isList;
    }

    read(): MusicChoices {
        return {
            SplitArtistTitle: this.elements.split.checked,
            CleanTitles: this.elements.clean.checked,
            NumberTracks: this.elements.number.checked,
            PlaylistAsAlbum: this.elements.album.checked,
            SquareCover: this.elements.square.checked,
        };
    }

    write(music: Partial<MusicChoices>): void {
        const set = (box: HTMLInputElement, value: boolean | undefined) => {
            if (value !== undefined) {
                box.checked = value;
            }
        };
        set(this.elements.split, music.SplitArtistTitle);
        set(this.elements.clean, music.CleanTitles);
        set(this.elements.number, music.NumberTracks);
        set(this.elements.album, music.PlaylistAsAlbum);
        set(this.elements.square, music.SquareCover);
    }
}
