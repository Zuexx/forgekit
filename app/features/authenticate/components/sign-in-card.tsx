"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Image from "next/image"
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
          {/* <CardDescription>
          </CardDescription>  */}
        </CardHeader>
        <CardContent>
          <form noValidate onSubmit={form.handleSubmit(onSubmit)}>
            <FieldGroup>
              <Field>
                <Button variant="outline" type="button" onClick={onSocialSignIn}>
                  <svg width="24" height="24" viewBox="0 0 24 24" className="mr-2 antialiased" fill="none" xmlns="http://www.w3.org/2000/svg" shapeRendering="geometricPrecision" preserveAspectRatio="xMidYMid meet">
                    <circle cx="11.25" cy="11.25" r="11.25" fill="black" />
                    <path d="M11.483 1.75 H15.75 L11.620 16.123 C11.492 16.697 11.515 17.224 12.312 17.224 C13.158 17.224 14.428 17.172 14.428 17.172 L13.548 20.531 C13.548 20.531 11.951 20.768 10.788 20.749 C7.973 20.704 7.043 19.734 7.001 18.097 C6.991 17.727 7.060 17.267 7.168 16.757 L11.483 1.75 Z" fill="white" vectorEffect="non-scaling-stroke" />
                  </svg>
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
