"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { useTranslations } from 'next-intl'
import {
  Controller,
  useForm
} from "react-hook-form"
import { z } from "zod"

import { InputField } from "@/components/form-fields/input-field"
import { PasswordField } from "@/components/form-fields/password-field"
import { LocaleSwitcher } from "@/components/locale-switcher"
import { Logo, LogoMark } from "@/components/logo"
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
  FieldSeparator,
} from "@/components/ui/field"
import { createSignInSchema, useSignIn, useSocialSignIn } from "@/features/authenticate"
import { cn } from "@/lib/utils"

export function SignInCard({
  className,
  ...props
}: React.ComponentProps<"div">) {
  const tAuth = useTranslations("auth.signIn")
  const tCommon = useTranslations("common")
  const tForm = useTranslations("form")
  const tValidation = useTranslations("validation")
  const SignInSchema = createSignInSchema(tValidation)

  const signIn = useSignIn()
  const socialSignIn = useSocialSignIn()

  const form = useForm<z.infer<typeof SignInSchema>>({
    resolver: zodResolver(SignInSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  })

  const onSubmit = (values: z.infer<typeof SignInSchema>) => {
    signIn.mutate({ json: values })
  }

  const onSocialSignIn = () => {
    socialSignIn.mutate()
  }

  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card className="bg-card/60 backdrop-blur-sm border-border">
        <CardHeader className="flex flex-col justify-center relative">
          <div className="absolute -top-4 right-2 gap-2 z-10 flex place-items-center">
            <ThemeSwitcher />
            <LocaleSwitcher />
          </div>
          <Logo className="h-14 w-auto text-foreground" role="img" aria-label="App Logo" />
          <CardTitle className="text-xl">
            {tAuth('subtitle')}
          </CardTitle>
          {/* <CardDescription>
          </CardDescription>  */}
        </CardHeader>
        <CardContent>
          <form noValidate onSubmit={form.handleSubmit(onSubmit)}>
            <FieldGroup>
              <Field>
                <Button variant="outline" type="button" onClick={onSocialSignIn}>
                  <LogoMark className="mr-2 h-6 w-6 text-foreground" aria-hidden="true" />
                  {tAuth("loginWithSSO")}
                </Button>
              </Field>
              <FieldSeparator className="*:data-[slot=field-separator-content]:bg-card/60 *:data-[slot=field-separator-content]:px-3 text-sm text-muted-foreground">
                {tAuth('orContinueWith')}
              </FieldSeparator>
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
                <div className="flex items-center">
                  <FieldLabel htmlFor="password">{tForm('password.label')}</FieldLabel>
                  <Link href="/forgot-password" className="ml-auto text-sm hover:underline hover:text-primary">
                    {tAuth('forgotPassword')}
                  </Link>
                </div>
                <PasswordField
                  control={form.control}
                  name="password"
                  placeholder={tForm("password.placeholder")}
                  disabled={false}
                />
              </Field>
              <Field>
                <Button type="submit">{tAuth('loginButton')}</Button>
                <FieldDescription className="text-center">
                  <>{tAuth('signupPrompt')} <Link href="/sign-up">{tAuth('signupCTA')}</Link></>
                </FieldDescription>
              </Field>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
      <FieldDescription className="px-6 text-center">
        <>{tCommon("tos.prefix")} <a href="#">{tCommon("tos.terms")}</a> {tCommon("tos.and")} <Link href="/sign-up">{tCommon("tos.privacy")}</Link>.</>
      </FieldDescription>
    </div >
  )
}
