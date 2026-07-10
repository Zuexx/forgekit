"use client"
import { NextIntlClientProvider } from "next-intl";
import { ReactNode } from "react";

interface TranslationsProviderProps {
    children: ReactNode;
    locale: string;
    messages: Record<string, any>;
}

export function TranslationsProvider({
    children,
    locale,
    messages
}: TranslationsProviderProps) {
    return (
        <NextIntlClientProvider locale={locale} messages={messages}>
            {children}
        </NextIntlClientProvider>
    );
}
