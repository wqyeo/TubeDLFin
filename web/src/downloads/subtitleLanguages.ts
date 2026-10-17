/** Splits "en, de" into ["en", "de"]. */
export function parseLanguages(text: string): string[] {
    return text
        .split(',')
        .map((code) => code.trim())
        .filter((code) => code.length > 0);
}

/** Adds the code if missing, removes it if present: what clicking a language chip does. */
export function toggleLanguage(text: string, code: string): string {
    const codes = parseLanguages(text);
    return (codes.includes(code) ? codes.filter((c) => c !== code) : [...codes, code]).join(',');
}
