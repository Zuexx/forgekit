// Vitest runs test files directly under Node, not through Next.js's webpack build, so it has
// none of Next's special-cased resolution that swaps "server-only" for a no-op in a server
// compilation and only leaves its throw for a client one. Left unaliased, `import "server-only"`
// always throws under Vitest, even from a file that is genuinely only ever loaded server-side --
// this stub is aliased in for exactly that import, in vitest.config.mts, to restore Next's
// server-side behavior (a no-op) in the one environment where it can't tell them apart.
export {}
