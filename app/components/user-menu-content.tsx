"use client"

import type { InferResponseType } from "hono/client"
import {
  Bell,
  History,
  LogOut,
  RotateCcw,
  User,
} from "lucide-react"
import { useRouter } from "next/navigation"

import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "@/components/ui/avatar"
import {
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { useSignOut } from "@/features/authenticate"
import { rpcClient } from "@/lib/rpc"
import type { User as StoreUser } from "@/lib/store"

type MeResponse = NonNullable<InferResponseType<typeof rpcClient.api.authenticate.me.$get, 200>["data"]>

export function getInitials(name: string) {
  if (!name) return "U"

  // 處理多語言字元 (中文、韓文、日文等)
  const trimmedName = name.trim()
  
  // 如果是單個詞（可能是中文、韓文等單字名）
  if (!trimmedName.includes(" ")) {
    // 取前兩個字元（對於中文/韓文/日文名字）
    return trimmedName.substring(0, 2).toUpperCase()
  }

  // 對於多個詞的名字（英文名字或有空格的名字）
  const words = trimmedName.split(" ").filter(word => word.length > 0)
  
  if (words.length === 1) {
    // 只有一個詞，取前兩個字元
    return words[0].substring(0, 2).toUpperCase()
  }
  
  // 多個詞：取每個詞的第一個字元，最多兩個
  const initials = words
    .slice(0, 2)
    .map((word) => word[0])
    .join("")
    .toUpperCase()

  return initials
}

type UserMenuContentProps = {
  user: StoreUser & { image?: string | null }
}

export const UserMenuContent = ({ user }: UserMenuContentProps) => {
  const router = useRouter()
  const signOut = useSignOut()

  const handleSignOut = async () => {
    try {
      await signOut.mutateAsync()
    } catch (e) {
      console.error(e)
    }
  }

  return (
    <>
      <DropdownMenuLabel className="p-0 font-normal">
        <div className="flex items-center gap-2 px-1 py-1.5 text-left text-sm">
          <Avatar className="h-8 w-8 rounded-lg">
            <AvatarImage src={user.avatar || user.image || undefined} alt={user.name} />
            <AvatarFallback className="rounded-lg">{getInitials(user.name)}</AvatarFallback>
          </Avatar>
          <div className="grid flex-1 text-left text-sm leading-tight">
            <span className="truncate font-medium">{user.name}</span>
            <span className="truncate text-xs">{user.email}</span>
          </div>
        </div>
      </DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuGroup>
        <DropdownMenuItem onSelect={() => router.push("/user/profile")}>
          <User />
          Profile
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => router.push("/user/notification")}>
          <Bell />
          Notifications
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => router.push("/user/trained-document")}>
          <History />
          Training History
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => router.push("/user/change-password")}>
          <RotateCcw />
          Change Password
        </DropdownMenuItem>
      </DropdownMenuGroup>
      <DropdownMenuSeparator />
      <DropdownMenuItem onSelect={handleSignOut}>
        <LogOut />
        Sign out
      </DropdownMenuItem>
    </>
  )
}
