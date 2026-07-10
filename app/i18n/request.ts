import { hasLocale } from "next-intl"
import { getRequestConfig } from 'next-intl/server'

import { routing } from "@/i18n"
import { getMessages } from "@/messages"

export default getRequestConfig(async ({ requestLocale }) => {
    const requested = await requestLocale
    const locale =
        hasLocale(routing.locales, requested)
            ? requested
            : routing.defaultLocale
    
    return {
        locale,
        messages: await getMessages(locale),
        timeZone: 'Asia/Taipei'
    }
})