import { defineConfig } from "vitest/config"

export default defineConfig({
    resolve: {
        // Resolves the "@/*" aliases declared in tsconfig.json.
        tsconfigPaths: true,
    },
    test: {
        environment: "node",
        include: ["**/*.test.ts", "**/*.test.tsx"],
        exclude: ["node_modules/**", ".next/**"],
    },
})
