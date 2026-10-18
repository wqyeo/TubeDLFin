import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { formLayout } from './formLayout';

describe('formLayout', () => {
    it('downloads a single video, with no choice', () => {
        const layout = formLayout(false, 'subscribe', true);

        assert.deepEqual(
            [layout.title, layout.showChoice, layout.showSubscribe, layout.showItems, layout.submitLabel],
            ['Download', false, false, false, 'Download'],
        );
    });

    it('offers the choice for a list, downloading it by default', () => {
        const layout = formLayout(true, 'download', false);

        assert.deepEqual(
            [
                layout.showChoice,
                layout.showSubscribe,
                layout.showItems,
                layout.showRedownload,
                layout.submitLabel,
            ],
            [true, false, true, true, 'Download'],
        );
    });

    it('subscribes without items unless the videos it has now are wanted too', () => {
        const only = formLayout(true, 'subscribe', false);
        const both = formLayout(true, 'subscribe', true);

        assert.deepEqual(
            [
                only.title,
                only.showSubscribe,
                only.showItems,
                only.showRedownload,
                only.submitLabel,
                only.busyLabel,
            ],
            ['Subscribe', true, false, false, 'Subscribe', 'Subscribing…'],
        );
        assert.deepEqual([both.showItems, both.submitLabel], [true, 'Subscribe and download']);
    });
});
