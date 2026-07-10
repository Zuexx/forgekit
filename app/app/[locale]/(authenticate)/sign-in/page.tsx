import { setRequestLocale } from "next-intl/server"
import { use } from "react"

import { SignInCard } from "@/features/authenticate/components/sign-in-card"

type SignInPageProps = {
  params: Promise<{ locale: string }>
}

export default function SignInPage({ params }: SignInPageProps) {
  const { locale } = use(params)
  setRequestLocale(locale)

  const envStart = process.env.NEXT_PUBLIC_MASK_START_OPACITY ?? process.env.MASK_START_OPACITY
  const envEnd = process.env.NEXT_PUBLIC_MASK_END_OPACITY ?? process.env.MASK_END_OPACITY

  const maskStartOpacity = envStart ? parseFloat(envStart) : 0.85
  const maskEndOpacity = envEnd ? parseFloat(envEnd) : 0.0

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-background">
      <video
        key={locale}
        className="absolute inset-0 w-full h-full object-cover"
        src={`/${locale}.mp4`}
        autoPlay
        muted
        loop
        playsInline
        preload="auto"
        aria-hidden="true"
      />
      <div
        className="absolute inset-0 bg-linear-to-t from-background/85 to-background/0"
        style={{
          '--mask-start': maskStartOpacity,
          '--mask-end': maskEndOpacity,
        } as React.CSSProperties}
        aria-hidden="true"
      />
      <div className="relative w-full max-w-md p-8">
        <SignInCard />
      </div>
    </div>
  )
}
