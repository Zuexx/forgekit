import { type QueryKey, useQuery } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { useEffect, useRef } from "react"
import toast from "react-hot-toast"

interface UseApiDependentQueryConfig<TData, TDependency, TError = Error> {
    queryKey: QueryKey
    queryFn: (dependency: TDependency) => Promise<TData>
    dependency: TDependency | undefined | null
    onSuccessCallback?: (data: TData) => void
    onErrorCallback?: (error: TError) => void
    showErrorToast?: boolean
    staleTime?: number
    gcTime?: number
    refetchOnWindowFocus?: boolean
    refetchOnMount?: boolean | "always"
    refetchOnReconnect?: boolean | "always"
    retry?: boolean | number
    retryDelay?: number
    networkMode?: "online" | "always" | "offlineFirst"
}

export const useApiDependentQuery = <TData, TDependency, TError = Error>({
    queryKey,
    queryFn,
    dependency,
    onSuccessCallback,
    onErrorCallback,
    showErrorToast = true,
    staleTime = 0,
    gcTime,
    refetchOnWindowFocus,
    refetchOnMount,
    refetchOnReconnect,
    retry,
    retryDelay,
    networkMode,
}: UseApiDependentQueryConfig<TData, TDependency, TError>) => {
    const t = useTranslations("toast")
    const previousDataRef = useRef<TData | undefined>(undefined)
    const previousErrorRef = useRef<TError | undefined>(undefined)

    const query = useQuery<TData, TError>({
        queryKey,
        queryFn: () => queryFn(dependency!),
        enabled: !!dependency,
        staleTime,
        gcTime,
        refetchOnWindowFocus,
        refetchOnMount,
        refetchOnReconnect,
        retry,
        retryDelay,
        networkMode,
    })

    useEffect(() => {
        if (query.data && query.data !== previousDataRef.current) {
            previousDataRef.current = query.data
            onSuccessCallback?.(query.data)
        }
    }, [query.data, onSuccessCallback])

    useEffect(() => {
        if (query.error && query.error !== previousErrorRef.current) {
            previousErrorRef.current = query.error
            if (showErrorToast) {
                toast.error(t("error.default"))
            }
            onErrorCallback?.(query.error)
        }
    }, [query.error, onErrorCallback, showErrorToast, t])

    return query
}
