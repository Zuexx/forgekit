import { InferRequestType, InferResponseType } from "hono"
import { useTranslations } from "next-intl"

import { useApiMutation } from "@/lib/queries"
import { rpcClient } from "@/lib/rpc"
import { useAppStoreContext } from "@/providers/store-provider"

type ResponseType = InferResponseType<typeof rpcClient.api.authenticate.signOut.$post>

export const useSignOut = () => {
    const t = useTranslations("toast")
    const logout = useAppStoreContext((state) => state.logout)

    return useApiMutation<ResponseType, void>({
        mutationFn: async () => {
            const response =
                await rpcClient.api.authenticate.signOut.$post()

            if (!response.ok) {
                const errorText = await response.text()
                throw new Error(t("error.signOut"))
            }

            const result = await response.json()

            return result
        },
        successMessage: t("success.signOut"),
        errorMessage: t("error.signOut"),
        invalidateQueries: ["me"],
        refreshRouter: true,
        onSuccessCallback: () => {
            logout()
        }
    })
}
