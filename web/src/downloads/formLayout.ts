/** What the form under a detected link does: download it now, or subscribe to it. */
export type FormAction = 'download' | 'subscribe';

/** Which parts of the form show, and what its button says. */
export interface FormLayout {
    title: string;
    /** The "Download now / Subscribe" choice: only for a channel or playlist. */
    showChoice: boolean;
    /** Name, check interval and "also download what it has now". */
    showSubscribe: boolean;
    /** The playlist "Items" box. */
    showItems: boolean;
    /** "Download again even if already downloaded". */
    showRedownload: boolean;
    submitLabel: string;
    busyLabel: string;
}

/**
 * The form's layout for a detected link. Subscribing to a single video makes no sense, so a
 * video always downloads. Items are only asked for when something is downloaded now. Pure: no DOM.
 */
export function formLayout(isList: boolean, action: FormAction, includeExisting: boolean): FormLayout {
    const subscribing = isList && action === 'subscribe';
    if (!subscribing) {
        return {
            title: 'Download',
            showChoice: isList,
            showSubscribe: false,
            showItems: isList,
            showRedownload: true,
            submitLabel: 'Download',
            busyLabel: 'Adding…',
        };
    }

    return {
        title: 'Subscribe',
        showChoice: true,
        showSubscribe: true,
        showItems: includeExisting,
        showRedownload: false,
        submitLabel: includeExisting ? 'Subscribe and download' : 'Subscribe',
        busyLabel: 'Subscribing…',
    };
}
