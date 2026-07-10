
import { createMiddleware } from "hono/factory"

import { auth } from "@/lib/auth.config";

type AdditionContext = {
    Variables: {
        user: typeof auth.$Infer.Session.user | null
        session: typeof auth.$Infer.Session.session | null
        jwt: string | null
    }
}

export const sessionMiddleware = createMiddleware<AdditionContext>(
    async (c, next) => {
        // console.log(`Middleware invoked for: ${c.req.method} ${c.req.url}`);

        const session =
            await auth.api.getSession({ headers: c.req.raw.headers })

        if (!session) {
            c.set("user", null);
            c.set("session", null);
            c.set("jwt", null);

            return c.json({ error: "Unauthorized" }, 401)
        }

        c.set("user", session.user)
        c.set("session", session.session)

        const { token } =
            await auth.api.getToken({ headers: c.req.raw.headers });

        c.set("jwt", token);

        await next()
    }
)

