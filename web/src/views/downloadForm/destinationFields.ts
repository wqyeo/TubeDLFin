import type { LibraryInfo } from '../../api/types';
import { freeSpaceProblem } from '../../downloads/freeSpace';
import { element } from '../elements';
import { formatSize } from '../format';

/** Elements of the destination fields. */
export interface DestinationElements {
    library: HTMLSelectElement;
    folderContainer: HTMLElement;
    folder: HTMLSelectElement;
    subfolder: HTMLInputElement;
    freeSpace: HTMLElement;
}

/** The chosen destination. */
export interface Destination {
    libraryId: string;
    libraryFolder: string;
    subfolder: string;
}

/** Library, folder (when the library has several), subfolder, and the folder's free space. */
export class DestinationFields {
    private libraries: LibraryInfo[] = [];
    private estimate: number | undefined;
    private libraryChanged: (library: LibraryInfo) => void = () => {};
    private librarySelected: () => void = () => {};

    constructor(private readonly elements: DestinationElements) {
        elements.library.addEventListener('change', () => {
            this.fillFolders();
            const library = this.selectedLibrary();
            if (library) {
                this.libraryChanged(library);
            }
        });
        elements.folder.addEventListener('change', () => this.showFreeSpace());
    }

    /** Called whenever the selected library changes, by the admin or the code. */
    onLibrarySelected(handler: () => void): void {
        this.librarySelected = handler;
    }

    /** Called when the admin picks another library (not when the code sets one). */
    onLibraryChanged(handler: (library: LibraryInfo) => void): void {
        this.libraryChanged = handler;
    }

    setLibraries(libraries: LibraryInfo[]): void {
        const selected = this.elements.library.value;
        this.libraries = libraries;
        this.elements.library.replaceChildren(
            ...libraries.map((library) => option(library.Id, `${library.Name} (${typeLabel(library)})`)),
        );
        if (libraries.some((library) => library.Id === selected)) {
            this.elements.library.value = selected;
        }

        this.fillFolders();
    }

    get hasLibraries(): boolean {
        return this.libraries.length > 0;
    }

    selectedLibrary(): LibraryInfo | undefined {
        return this.libraries.find((library) => library.Id === this.elements.library.value);
    }

    /** Selects a library if it exists; returns the selected one. */
    selectLibrary(id: string | undefined): LibraryInfo | undefined {
        if (id && this.libraries.some((library) => library.Id === id)) {
            this.elements.library.value = id;
            this.fillFolders();
        }

        return this.selectedLibrary();
    }

    /** Sets the folder (if it is one of the library's) and the subfolder. */
    write(folder: string | undefined, subfolder: string): void {
        if (folder && [...this.elements.folder.options].some((o) => o.value === folder)) {
            this.elements.folder.value = folder;
        }

        this.elements.subfolder.value = subfolder;
        this.showFreeSpace();
    }

    read(): Destination {
        return {
            libraryId: this.elements.library.value,
            libraryFolder: this.elements.folder.value,
            subfolder: this.elements.subfolder.value,
        };
    }

    /** The expected size of the download, to compare with the free space. */
    setEstimate(bytes: number | undefined): void {
        this.estimate = bytes;
        this.showFreeSpace();
    }

    private fillFolders(): void {
        this.librarySelected();
        const folders = this.selectedLibrary()?.Folders ?? [];
        this.elements.folder.replaceChildren(...folders.map((folder) => option(folder.Path, folder.Path)));
        this.elements.folderContainer.hidden = folders.length < 2;
        this.showFreeSpace();
    }

    private showFreeSpace(): void {
        const folder = this.selectedLibrary()?.Folders.find((f) => f.Path === this.elements.folder.value);
        const free = folder?.FreeBytes;
        if (free == null) {
            this.elements.freeSpace.replaceChildren();
            return;
        }

        const problem = freeSpaceProblem(free, this.estimate);
        const estimate =
            this.estimate !== undefined ? ` · this download: about ${formatSize(this.estimate)}` : '';
        this.elements.freeSpace.replaceChildren(
            `${formatSize(free)} free${estimate}`,
            ...(problem ? [' ', element('span', problem, 'tubedlfin-warn')] : []),
        );
    }
}

function option(value: string, label: string): HTMLOptionElement {
    const node = element('option', label);
    node.value = value;
    return node;
}

function typeLabel(library: LibraryInfo): string {
    switch (library.Type) {
        case 'Shows':
            return 'shows';
        case 'MusicVideos':
            return 'music videos';
        case 'HomeVideos':
            return 'home videos';
        default:
            return library.Type.toLowerCase();
    }
}
