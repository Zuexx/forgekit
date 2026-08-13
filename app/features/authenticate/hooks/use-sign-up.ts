import { InferRequestType, InferResponseType } from "hono"
import { useTranslations } from "next-intl"

import { useApiMutation } from "@/lib/queries"
import { rpcClient } from "@/lib/rpc"

type ResponseType = InferResponseType<typeof rpcClient.api.authenticate.signUp.$post>
type RequestType = InferRequestType<typeof rpcClient.api.authenticate.signUp.$post>

export const useSignUp = () => {
    const t = useTranslations("toast")

    return useApiMutation<ResponseType, RequestType>({
        mutationFn: async (data) => {
            const response =
                await rpcClient.api.authenticate.signUp.$post(data)

            if (!response.ok) {
                throw new Error(t("error.signUp"))
            }

            return await response.json()
        },
        successMessage: t("success.signUp"),
        errorMessage: t("error.signUp"),
        invalidateQueries: ["me"],
        refreshRouter: true
    })
}
