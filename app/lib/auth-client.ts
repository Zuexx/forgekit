import { createAuthClient } from "better-auth/client"
import { adminClient } from "better-auth/client/plugins"

const { BETTER_AUTH_URL } = process.env
export const authClient = createAuthClient({
    baseURL: BETTER_AUTH_URL,
    plugins: [
        adminClient()
    ]
})