import { zValidator } from "@hono/zod-validator"
import { Hono } from "hono"

import { signInSchema } from "@/features/authenticate/schemas"
import { auth } from "@/lib/auth.config"
import { sessionMiddleware } from "@/lib/rpc/session-middleware"

const app = new Hono()
    .on(["POST", "GET"], "/auth/**", (c) => {
        return auth.handler(c.req.raw)
    })
    .get("/me",
        sessionMiddleware,
        (c) => {
            const user = c.get("user")
            return c.json({ data: user })
        }
    )
    .post(
        "/signIn",
        zValidator("json", signInSchema),
        async (c) => {
            const data =
                await auth.api.signInEmail({
                    body: await c.req.json()
                })

            return c.json({ data })
        }
    )
    .get("/social/microsoft",
        async (c) => {
            const data =
                await
                    auth.api.signInSocial({
                        body: {
                            provider:
                                "microsoft",
                        }
                    });

            if (data && "url" in data && data.url) {
                return c.redirect(data.url);
            }

            return c.redirect("/sign-in?error=social_login_failed");
        }
    )
    .post("/signOut",
        sessionMiddleware,
        async (c) => {
            const data =
                await auth.api.signOut({ headers: c.req.raw.headers })

            return c.json({ data })
        }
    )

export default app
