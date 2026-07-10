"use client"
import { ReactNode } from "react"
import { Toaster } from "react-hot-toast"

import { AppStoreProvider,QueryProvider, TranslationsProvider } from "@/providers"

interface AppProviderProps {
    children: ReactNode
    locale: string
    messages: Record<string, unknown>
}

export function AppProvider({
    children,
    locale,
    messages
}: AppProviderProps) {
    const providers: ((children: ReactNode, locale: string, messages: Record<string, unknown>) => ReactNode)[] = [
        (children) => <AppStoreProvider>{children}</AppStoreProvider>,
        (children) => <QueryProvider>{children}</QueryProvider>,
        (children, locale, messages) => (
            <TranslationsProvider locale={locale} messages={messages}>
                {children}
            </TranslationsProvider>
        ),
        (children) => (
            <>
                {children}
                <Toaster position="bottom-right" />
            </>
        ),
    ]

    const composed = providers.reduceRight((acc, wrap) => wrap(acc, locale, messages), children)

    return <>{composed}</>
}
