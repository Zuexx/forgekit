import { Hono } from "hono"
import { handle } from "hono/vercel"

import authenticateRoute from "@/features/authenticate/route"

const app =
    new Hono()
        .basePath("/api")

const routes = app
    .route("/authenticate", authenticateRoute)


export type AppType = typeof routes

export const GET = handle(app)
export const POST = handle(app)
export const PUT = handle(app)
export const DELETE = handle(app)
export const PATCH = handle(app)