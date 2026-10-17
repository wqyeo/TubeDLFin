// Minimal typings for the parts of Node's built-in test runner the unit tests use, so the web
// project needs no @types/node. Tests run in Node (see test.mjs), the page in the browser.

declare module 'node:test' {
    export function describe(name: string, fn: () => void): void;
    export function it(name: string, fn: () => void | Promise<void>): void;
}

declare module 'node:assert/strict' {
    interface Assert {
        equal(actual: unknown, expected: unknown, message?: string): void;
        deepEqual(actual: unknown, expected: unknown, message?: string): void;
        ok(value: unknown, message?: string): void;
    }

    const assert: Assert;
    export default assert;
}
