const locales = {
    en: () => import('./en'),
    'ko-KR': () => import('./ko-KR'),
    'zh-TW': () => import('./zh-TW'),
} as const;

export type Locale = keyof typeof locales;

export async function getMessages(locale: string) {
    const loader = locales[locale as Locale] || locales.en;
    const loadedModule = await loader();
    return loadedModule.messages;
}
