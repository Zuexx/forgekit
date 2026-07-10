import { type QueryKey, useQueries } from "@tanstack/react-query"

interface QueryConfig<TData, TError = Error> {
    queryKey: QueryKey
    queryFn: () => Promise<TData>
    enabled?: boolean
    staleTime?: number
    gcTime?: number
    refetchOnWindowFocus?: boolean
    refetchOnMount?: boolean | "always"
    refetchOnReconnect?: boolean | "always"
    retry?: boolean | number
    retryDelay?: number
    networkMode?: "online" | "always" | "offlineFirst"
}

interface UseApiQueriesConfig<TData, TError = Error> {
    queries: Array<QueryConfig<TData, TError>>
}

export const useApiQueries = <TData, TError = Error>({
    queries,
}: UseApiQueriesConfig<TData, TError>) => {
    return useQueries({
        queries: queries.map(query => ({
            queryKey: query.queryKey,
            queryFn: query.queryFn,
            enabled: query.enabled ?? true,
            staleTime: query.staleTime ?? 0,
            gcTime: query.gcTime,
            refetchOnWindowFocus: query.refetchOnWindowFocus,
            refetchOnMount: query.refetchOnMount,
            refetchOnReconnect: query.refetchOnReconnect,
            retry: query.retry,
            retryDelay: query.retryDelay,
            networkMode: query.networkMode,
        })),
    })
}
