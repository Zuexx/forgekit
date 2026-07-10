import { useMutation, type UseMutationOptions, useQueryClient } from "@tanstack/react-query"
import { useRouter } from "next/navigation"
import { useTranslations } from "next-intl"
import toast from "react-hot-toast"

interface UseApiMutationConfig<TData, TVariables> {
    mutationFn: (variables: TVariables) => Promise<TData>
    successMessage?: string
    errorMessage?: string
    invalidateQueries?: string[]
    onSuccessCallback?: (data: TData) => void
    onErrorCallback?: (error: Error) => void
    showSuccessToast?: boolean
    showErrorToast?: boolean
    refreshRouter?: boolean
}

export const useApiMutation = <TData, TVariables = void>({
    mutationFn,
    successMessage,
    errorMessage,
    invalidateQueries = [],
    onSuccessCallback,
    onErrorCallback,
    showSuccessToast = true,
    showErrorToast = true,
    refreshRouter = false,
}: UseApiMutationConfig<TData, TVariables>) => {
    const queryClient = useQueryClient()
    const router = useRouter()
    const t = useTranslations("toast")

    return useMutation({
        mutationFn: async (variables: TVariables) => {
            const result = await mutationFn(variables)
            return result
        },
        onSuccess: (data) => {
            if (showSuccessToast) {
                toast.success(successMessage || t("success.default"))
            }

            invalidateQueries.forEach((queryKey) => {
                queryClient.invalidateQueries({ queryKey: [queryKey] })
            })

            if (refreshRouter) {
                router.refresh()
            }

            onSuccessCallback?.(data)
        },
        onError: (error: Error) => {
            if (showErrorToast) {
                toast.error(errorMessage || t("error.default"))
            }

            onErrorCallback?.(error)
        }
    })
}
