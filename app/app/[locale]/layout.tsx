import "../globals.css"

import type { Metadata } from "next"
import { notFound } from 'next/navigation'
import { hasLocale } from 'next-intl'
import { getMessages, setRequestLocale } from 'next-intl/server'

import { routing } from '@/i18n'
import { AppProvider } from "@/providers"

export const metadata: Metadata = {
    title: "ForgeKit",
    description: "Full-stack starter kit for production-ready applications",
}

type Props = {
    children: React.ReactNode
    params: Promise<{ locale: string }>
}

export default async function LocaleLayout({ children, params }: Props) {
    const { locale } = await params
    if (!hasLocale(routing.locales, locale)) {
        notFound()
    }

    // Enable static rendering
    setRequestLocale(locale)

    const messages = await getMessages()

    return (
        <html lang={locale} suppressHydrationWarning>
            <head>
                <script
                    dangerouslySetInnerHTML={{
                        __html: `
                            (function() {
                                try {
                                    const theme = localStorage.getItem('theme') || 'light';
                                    if (theme === 'dark') {
                                        document.documentElement.classList.add('dark');
                                    }
                                } catch (e) {}
                            })();
                        `,
                    }}
                />
            </head>
            <body className="antialiased">
                <AppProvider locale={locale} messages={messages}>
                    {children}
                </AppProvider>
            </body>
        </html>
    )
}
