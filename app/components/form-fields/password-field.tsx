"use client"

import { Eye, EyeOff } from "lucide-react"
import { JSX, useState } from "react"
import { Control, Controller, FieldValues, Path } from "react-hook-form"

import { FieldDescription, FieldError } from "@/components/ui/field"
import { Input } from "@/components/ui/input"

interface PasswordFieldProps<T extends FieldValues> {
  name: Path<T>
  control: Control<T>
  placeholder?: string
  disabled?: boolean
  description?: string | JSX.Element
}

export const PasswordField = <T extends FieldValues>({
  name,
  control,
  placeholder,
  disabled = false,
  description
}: PasswordFieldProps<T>) => {
  const [showPassword, setShowPassword] = useState(false)

  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <>
          <div className="relative">
            <Input
              id={String(field.name)}
              {...field}
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              placeholder={placeholder}
              disabled={disabled}
            />
            <button
              type="button"
              className="absolute inset-y-0 right-0 flex cursor-pointer items-center p-3 text-muted-foreground hover:text-foreground transition-colors"
              onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? (
                <Eye className="h-5 w-5" />
              ) : (
                <EyeOff className="h-5 w-5" />
              )}
            </button>
          </div>
          {fieldState.error && <FieldError>{fieldState.error.message}</FieldError>}
          {description && <FieldDescription>{description}</FieldDescription>}
        </>
      )}
    />
  )
}
