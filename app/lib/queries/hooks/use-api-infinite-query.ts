import { type InfiniteData, type QueryKey, useInfiniteQuery } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { useEffect, useRef } from "react"
import toast from "react-hot-toast"

interface UseApiInfiniteQueryConfig<TData, TPageParam = unknown, TError = Error> {
    queryKey: QueryKey
    queryFn: (pageParam: TPageParam) => Promise<TData>
    initialPageParam: TPageParam
    getNextPageParam: (lastPage: TData, allPages: TData[], lastPageParam: TPageParam, allPageParams: TPageParam[]) => TPageParam | undefined | null
    getPreviousPageParam?: (firstPage: TData, allPages: TData[], firstPageParam: TPageParam, allPageParams: TPageParam[]) => TPageParam | undefined | null
    enabled?: boolean
    onSuccessCallback?: (data: InfiniteData<TData, TPageParam>) => void
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

export const useApiInfiniteQuery = <TData, TPageParam = unknown, TError = Error>({
    queryKey,
    queryFn,
    initialPageParam,
    getNextPageParam,
    getPreviousPageParam,
    enabled = true,
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
}: UseApiInfiniteQueryConfig<TData, TPageParam, TError>) => {
    const t = useTranslations("toast")
    const previousDataRef = useRef<InfiniteData<TData, TPageParam> | undefined>(undefined)
    const previousErrorRef = useRef<TError | undefined>(undefined)

    const query = useInfiniteQuery<TData, TError, InfiniteData<TData, TPageParam>, QueryKey, TPageParam>({
        queryKey,
        queryFn: ({ pageParam }) => queryFn(pageParam as TPageParam),
        initialPageParam,
        getNextPageParam,
        getPreviousPageParam,
        enabled,
        staleTime,
        gcTime,
        refetchOnWindowFocus,
        refetchOnMount,
        refetchOnReconnect,
        retry,
        retryDelay,
        networkMode,
        maxPages,
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
