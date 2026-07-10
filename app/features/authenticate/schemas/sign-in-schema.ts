import type { useTranslations } from "next-intl"
import { z } from "zod"

type TranslationFn = ReturnType<typeof useTranslations<"validation">>

// Validation constants (DRY - single source of truth)
const EMAIL_MIN_LENGTH = 1
const PASSWORD_MIN_LENGTH = 8

// Server-side schema (extends base with English error messages)
export const signInSchema = z.object({
    email: z.email("Invalid email address")
        .min(EMAIL_MIN_LENGTH, "Email is required"),
    password: z.string()
        .min(PASSWORD_MIN_LENGTH, `Password must be at least ${PASSWORD_MIN_LENGTH} characters`)
})

// Client-side schema factory (extends base with i18n error messages)
export const createSignInSchema = (t: TranslationFn) => {
    return signInSchema
        .extend({
            email: z.email(t("authenticate.email.invalid"))
                .min(EMAIL_MIN_LENGTH, t("authenticate.email.required")),
            password: z.string()
                .min(PASSWORD_MIN_LENGTH, t("authenticate.password.min", { min: PASSWORD_MIN_LENGTH }))
        })
}

export type SignInInput = z.infer<typeof signInSchema>
