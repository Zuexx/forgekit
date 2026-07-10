import { keepPreviousData, type QueryKey, useQuery } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { useEffect, useRef } from "react"
import toast from "react-hot-toast"

interface UseApiPaginatedQueryConfig<TData, TError = Error> {
    queryKey: QueryKey
    queryFn: (page: number) => Promise<TData>
    page: number
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
    maxPages?: number
}

export const useApiPaginatedQuery = <TData, TError = Error>({
    queryKey,
    queryFn,
    page,
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
    maxPages,
}: UseApiPaginatedQueryConfig<TData, TError>) => {
    const t = useTranslations("toast")
    const previousDataRef = useRef<TData | undefined>(undefined)
    const previousErrorRef = useRef<TError | undefined>(undefined)

    const query = useQuery<TData, TError>({
        queryKey: [...queryKey, page],
        queryFn: () => queryFn(page),
        staleTime,
        gcTime,
        refetchOnWindowFocus,
        refetchOnMount,
        refetchOnReconnect,
        retry,
        retryDelay,
        networkMode,
        placeholderData: keepPreviousData,
        ...(maxPages && { enabled: page <= maxPages }),
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
