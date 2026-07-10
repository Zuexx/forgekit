import type { InferResponseType } from "hono/client"
import { useTranslations } from "next-intl"
import toast from "react-hot-toast"

import { useApiQuery } from "@/lib/queries"
import { rpcClient } from "@/lib/rpc"
import { useAppStoreContext } from "@/providers/store-provider"

type ResponseType = InferResponseType<typeof rpcClient.api.authenticate.me.$get, 200>["data"]

interface UseMeConfig {
    successMessage?: string
    errorMessage?: string
    showSuccessToast?: boolean
    showErrorToast?: boolean
    onSuccessCallback?: (data: ResponseType) => void
    onErrorCallback?: (error: Error) => void
}

export const useMe = ({
    successMessage,
    errorMessage,
    showSuccessToast = false,
    showErrorToast = false,
    onSuccessCallback,
    onErrorCallback,
}: UseMeConfig = {}) => {
    const t = useTranslations("toast")
    const setUser = useAppStoreContext((state) => state.setUser)

    return useApiQuery<ResponseType>({
        queryKey: ["me"],
        queryFn: async () => {
            const response = await rpcClient.api.authenticate.me.$get()

            if (!response.ok) return null

            const { data } = await response.json()
            return data
        },
        staleTime: 0,
        showErrorToast: false,
        onSuccessCallback: (data) => {
            if (data) {
                setUser({
                    id: data.id,
                    name: data.name,
                    email: data.email,
                    avatar: data.image || undefined
                })
            } else {
                setUser(null)
            }
            
            if (showSuccessToast) {
                toast.success(successMessage || t("success.fetchCurrent"))
            }
            onSuccessCallback?.(data)
        },
        onErrorCallback: (error) => {
            setUser(null)
            if (showErrorToast) {
                toast.error(errorMessage || t("error.fetchCurrent"))
            }
            onErrorCallback?.(error)
        },
    })
}
