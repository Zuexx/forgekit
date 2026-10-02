"use client"

import { Languages } from "lucide-react"
import { usePathname, useRouter } from "next/navigation"
import { useLocale } from "next-intl"

import RadialMenu from "@/components/radial-menu"
import { routingConfig } from "@/i18n/config"

const localeLabels: Record<string, { short: string; full: string }> = {
  en: { short: "EN", full: "English" },
  "zh-TW": { short: "中", full: "繁體中文" },
}

// next-intl's middleware applies this cookie as a side effect of redirecting an
// explicit-locale URL to its canonical "as-needed" form, but that redirect and the
// plain-path request right behind it (e.g. the default locale's unprefixed URL) can
// both be in flight before the browser has applied the middleware's Set-Cookie -- the
// second request still carries the stale cookie, and localeDetection then bounces the
// unprefixed URL right back to the old locale. Setting it here, synchronously, before
// navigating is next-intl's own documented fix for exactly this race.
const setLocaleCookie = (locale: string) => {
  document.cookie = `NEXT_LOCALE=${locale}; path=/; max-age=31536000; samesite=lax`
}

type LocaleSwitcherProps = {
  arc?: number
  startAngle?: number
}

export const LocaleSwitcher = ({ arc = 37.5, startAngle = 340 }: LocaleSwitcherProps = {}) => {
  const locale = useLocale()
  const router = useRouter()
  const pathname = usePathname()

  const handleLocaleChange = (newLocale: string) => {
    const segments = pathname.split("/").filter(Boolean)
    const currentLocaleInPath = routingConfig.locales.find((loc) =>
      pathname.startsWith(`/${loc}`)
    )

    if (currentLocaleInPath) {
      segments[0] = newLocale
    } else {
      segments.unshift(newLocale)
    }

    const newPath = `/${segments.join("/")}`
    setLocaleCookie(newLocale)
    router.push(newPath)
  }

  const items = routingConfig.locales.map((loc) => ({
    label: localeLabels[loc]?.short || loc.toUpperCase(),
    onClick: () => handleLocaleChange(loc),
  }))

  return (
    <RadialMenu
      items={items}
      toggle={<Languages className="h-5 w-5" />}
      toggleAriaLabel="Change language"
      trigger="click"
      arc={arc}
      startAngle={startAngle}
    />
  )
}
