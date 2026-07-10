import { InferRequestType, InferResponseType } from "hono"
import { useTranslations } from "next-intl"

import { useApiMutation } from "@/lib/queries"
import { rpcClient } from "@/lib/rpc"

type ResponseType = InferResponseType<typeof rpcClient.api.authenticate.signIn.$post>
type RequestType = InferRequestType<typeof rpcClient.api.authenticate.signIn.$post>

export const useSignIn = () => {
    const t = useTranslations("toast")

    return useApiMutation<ResponseType, RequestType>({
        mutationFn: async (data) => {
            const response =
                await rpcClient.api.authenticate.signIn.$post(data)

            if (!response.ok) {
                const errorText = await response.text()
                throw new Error(t("error.signIn"))
            }

            const result = await response.json()

            return result
        },
        successMessage: t("success.signIn"),
        errorMessage: t("error.signIn"),
        invalidateQueries: ["me"],
        refreshRouter: true
    })
}
