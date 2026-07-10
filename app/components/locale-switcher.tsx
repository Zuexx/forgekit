"use client"

import { Languages } from "lucide-react"
import { usePathname, useRouter } from "next/navigation"
import { useLocale } from "next-intl"

import RadialMenu from "@/components/radial-menu"
import { routingConfig } from "@/i18n/config"

const localeLabels: Record<string, { short: string; full: string }> = {
  en: { short: "EN", full: "English" },
  "zh-TW": { short: "中", full: "繁體中文" },
  "ko-KR": { short: "한", full: "한국어" },
}

type LocaleSwitcherProps = {
  arc?: number
  startAngle?: number
}

export const LocaleSwitcher = ({ arc = 75, startAngle = 340 }: LocaleSwitcherProps = {}) => {
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
