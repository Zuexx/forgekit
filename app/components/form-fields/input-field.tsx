"use client"

import { JSX } from "react"
import { Control, Controller, FieldValues, Path } from "react-hook-form"

import { FieldDescription, FieldError } from "@/components/ui/field"
import { Input } from "@/components/ui/input"

interface InputFieldProps<T extends FieldValues> {
    name: Path<T>
    control: Control<T>
    type?: string
    placeholder?: string
    disabled?: boolean
    readOnly?: boolean
    description?: string | JSX.Element
}

export const InputField = <T extends FieldValues>({
    name,
    control,
    type = "text",
    placeholder,
    disabled = false,
    readOnly = false,
    description
}: InputFieldProps<T>) => {
    return (
        <Controller
            name={name}
            control={control}
            render={({ field, fieldState }) => (
                <>
                    <Input
                        id={String(field.name)}
                        {...field}
                        value={field.value ?? ""}
                        type={type}
                        placeholder={placeholder}
                        disabled={disabled}
                        readOnly={readOnly}
                        min={type === "number" ? 0 : undefined}
                        onKeyDown={(event) => {
                            if (type === "number" && ["+", "-", "e", "E"].includes(event.key)) {
                                event.preventDefault();
                            }
                        }}
                    />
                    {fieldState.error && <FieldError>{fieldState.error.message}</FieldError>}
                    {description && <FieldDescription>{description}</FieldDescription>}
                </>
            )}
        />
    )
}
