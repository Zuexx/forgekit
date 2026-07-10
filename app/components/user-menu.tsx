"use client"

import { ChevronsUpDown } from "lucide-react"

import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Skeleton } from "@/components/ui/skeleton"
import { getInitials, UserMenuContent } from "@/components/user-menu-content"
import { useMe } from "@/features/authenticate/hooks/us-me"
import { useAppStoreContext } from "@/providers/store-provider"

export const UserMenu = () => {
  const user = useAppStoreContext((state) => state.user)
  const isAuthenticated = useAppStoreContext((state) => state.isAuthenticated)
  const { isLoading } = useMe()

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 px-2">
        <Skeleton className="h-8 w-8 rounded-lg" />
        <div className="hidden md:flex flex-col gap-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-3 w-32" />
        </div>
      </div>
    )
  }

  if (!isAuthenticated || !user) {
    return null
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className="flex items-center gap-2 px-2"
        >
          <Avatar className="h-8 w-8 rounded-lg">
            <AvatarImage src={user.avatar} alt={user.name} />
            <AvatarFallback className="rounded-lg">{getInitials(user.name)}</AvatarFallback>
          </Avatar>
          <div className="hidden md:grid flex-1 text-left text-sm leading-tight">
            <span className="truncate font-medium">{user.name}</span>
            <span className="truncate text-xs text-muted-foreground">{user.email}</span>
          </div>
          <ChevronsUpDown className="hidden md:block ml-auto size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        className="w-56 rounded-lg"
        align="end"
        sideOffset={8}
      >
        <UserMenuContent user={user} />
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
