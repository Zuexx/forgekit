import { useTranslations } from "next-intl"

export const useSocialSignIn = () => {
    const t = useTranslations("toast")

    return {
        mutate: () => {
            // For OAuth flows, we need direct navigation, not fetch
            // The endpoint will redirect to Microsoft OAuth
            window.location.href = "/api/authenticate/social/microsoft"
        },
        isLoading: false,
        isError: false,
        isSuccess: false
    }
}
