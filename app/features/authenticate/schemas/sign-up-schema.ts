import type { useTranslations } from "next-intl"
import { z } from "zod"

type TranslationFn = ReturnType<typeof useTranslations<"validation">>

// Validation constants (DRY - single source of truth)
const NAME_MIN_LENGTH = 1
const EMAIL_MIN_LENGTH = 1
const PASSWORD_MIN_LENGTH = 8

/**
 * Both schemas are built from this shape and add their own passwords-match check.
 *
 * The alternative — extending an already-refined schema — stacks refinements rather than
 * replacing them, so a second check does not override the first. Two checks that cannot
 * both hold reject every input.
 */
const shape = {
    name: z.string().min(NAME_MIN_LENGTH, "Name is required"),
    email: z.email("Invalid email address").min(EMAIL_MIN_LENGTH, "Email is required"),
    password: z.string()
        .min(PASSWORD_MIN_LENGTH, `Password must be at least ${PASSWORD_MIN_LENGTH} characters`),
    confirmPassword: z.string("Confirm password is required"),
}

const passwordsMatch = (data: { password: string; confirmPassword: string }) =>
    data.password === data.confirmPassword

// Server-side schema (English error messages)
export const signUpSchema = z.object(shape).refine(passwordsMatch, {
    path: ["confirmPassword"],
    message: "Passwords must match",
})

// Client-side schema factory (same rules, i18n error messages)
export const createSignUpSchema = (t: TranslationFn) =>
    z.object({
        ...shape,
        name: z.string().min(NAME_MIN_LENGTH, t("authenticate.name.required")),
        email: z.email(t("authenticate.email.invalid"))
            .min(EMAIL_MIN_LENGTH, t("authenticate.email.required")),
        password: z.string()
            .min(PASSWORD_MIN_LENGTH, t("authenticate.password.min", { min: PASSWORD_MIN_LENGTH })),
        confirmPassword: z.string(t("authenticate.confirmPassword.required")),
    }).refine(passwordsMatch, {
        path: ["confirmPassword"],
        message: t("authenticate.confirmPassword.confirm"),
    })

export type SignUpInput = z.infer<typeof signUpSchema>
