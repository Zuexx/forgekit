import { fileURLToPath } from "url"

import { defineConfig } from "vitest/config"

export default defineConfig({
    resolve: {
        // Resolves the "@/*" aliases declared in tsconfig.json.
        tsconfigPaths: true,
        alias: {
            // See vitest.server-only-stub.ts for why this alias exists.
            "server-only": fileURLToPath(
                new URL("./vitest.server-only-stub.ts", import.meta.url),
            ),
        },
    },
    test: {
        environment: "node",
        include: ["**/*.test.ts", "**/*.test.tsx"],
        exclude: ["node_modules/**", ".next/**", "e2e/**"],
    },
})
