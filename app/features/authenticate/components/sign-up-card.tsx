"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Image from "next/image"
import Link from "next/link"
import { useTranslations } from "next-intl"
import { useForm } from "react-hook-form"
import z from "zod"

import { InputField } from "@/components/form-fields/input-field"
import { PasswordField } from "@/components/form-fields/password-field"
import { LocaleSwitcher } from "@/components/locale-switcher"
import { ThemeSwitcher } from "@/components/theme-switcher"
import { Button } from "@/components/ui/button"
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import {
    Field,
    FieldDescription,
    FieldGroup,
    FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { createSignUpSchema } from "@/features/authenticate"
import { cn } from "@/lib/utils"

export function SingUpCard({
    className,
    ...props
}: React.ComponentProps<"div">) {

    const tAuth = useTranslations("auth.signUp")
    const tCommon = useTranslations("common")
    const tForm = useTranslations("form")
    const tValidation = useTranslations("validation")
    const SignUpSchema = createSignUpSchema(tValidation)

    //   const signIn = useSignIn()

    const form = useForm<z.infer<typeof SignUpSchema>>({
        resolver: zodResolver(SignUpSchema),
        defaultValues: {
            name: "",
            email: "",
            password: "",
            confirmPassword: "",
        },
    })

    const onSubmit = (values: z.infer<typeof SignUpSchema>) => {
        console.log("Sign Up values:", values)
        // signUp.mutate({ json: values })
    }

    return (
        <div className={cn("flex flex-col gap-6", className)} {...props}>
            <Card className="bg-card/60 backdrop-blur-sm border-border">
                <CardHeader className="flex flex-col justify-center relative">
                    <div className="absolute -top-4 right-2 gap-2 z-10 flex place-items-center">
                        <ThemeSwitcher />
                        <LocaleSwitcher />
                    </div>
                    <Image
                        src={"/logo.svg"}
                        height={56}
                        width={152}
                        alt={"App Logo"}
                        priority
                        className="h-auto w-auto"
                    />
                    <CardTitle className="text-xl">
                        {tAuth('subtitle')}
                    </CardTitle>
                    <CardDescription>
                        {tAuth('signUpDesc')}
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <form noValidate onSubmit={form.handleSubmit(onSubmit)}>
                        <FieldGroup>
                            <Field>
                                <FieldLabel htmlFor="name">{tForm("fullName.label")}</FieldLabel>
                                <InputField
                                    control={form.control}
                                    name="name"
                                    type="text"
                                    placeholder={tForm("fullName.placeholder")}
                                />
                            </Field>
                            <Field>
                                <FieldLabel htmlFor="email">{tForm("email.label")}</FieldLabel>
                                <InputField
                                    control={form.control}
                                    name="email"
                                    type="email"
                                    placeholder={tForm('email.placeholder')}
                                />
                            </Field>
                            <Field>
                                <Field className="grid grid-cols-2 gap-4">
                                    <Field>
                                        <FieldLabel htmlFor="password">{tForm("signUp.password.label")}</FieldLabel>
                                        <PasswordField
                                            control={form.control}
                                            name="password"
                                            placeholder={tForm("signUp.password.placeholder")}
                                            disabled={false} />
                                    </Field>
                                    <Field>
                                        <FieldLabel htmlFor="confirmPassword">
                                            {tForm("signUp.confirmPassword.label")}
                                        </FieldLabel>
                                        <PasswordField
                                            control={form.control}
                                            name="confirmPassword"
                                            placeholder={tForm("signUp.confirmPassword.placeholder")}
                                            disabled={false} />
                                    </Field>
                                </Field>
                                <FieldDescription>
                                    {tAuth("passwordHint")}
                                </FieldDescription>
                            </Field>
                            <Field>
                                <Button type="submit">{tAuth('createAccountButton')}</Button>
                                <FieldDescription className="text-center">
                                    {tAuth("signinPrompt")} <Link href="/sign-in">{tAuth("signinCTA")}</Link>
                                </FieldDescription>
                            </Field>
                        </FieldGroup>
                    </form>
                </CardContent>
            </Card>
            <FieldDescription className="px-6 text-center">
                {tCommon("tos.prefix")} <a href="#">{tCommon("tos.terms")}</a>{" "}
                {tCommon("tos.and")} <a href="#">{tCommon("tos.privacy")}</a>.
            </FieldDescription>
        </div>
    )
}
