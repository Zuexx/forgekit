import { hc } from "hono/client"

import { AppType } from "@/app/api/[[...hono]]/route"

const getBaseUrl = () => {
  if (typeof window !== "undefined") {
    // Browser: use relative path
    return ""
  }
  
  // Server-side: use environment variable or localhost
  if (process.env.NEXT_PUBLIC_APP_URL) {
    return process.env.NEXT_PUBLIC_APP_URL
  }
  
  if (process.env.VERCEL_URL) {
    return `https://${process.env.VERCEL_URL}`
  }
  
  return "http://localhost:3000"
}

export const rpcClient = hc<AppType>(getBaseUrl())