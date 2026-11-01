# Domain Docs

How the engineering skills (`/grill-with-docs`, `/domain-modeling`, `/to-spec`) read and write
this repo's domain documentation. Single-context layout.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root: the product's domain language. It does not exist in the
  base kit; `/domain-modeling` creates it when the first product term is resolved. It holds
  domain terms only, no implementation details.
- **`docs/adr/`**: read the ADRs that touch the area you are about to work in.
- **`docs/api/GLOSSARY.md`** is the *technical* glossary of the platform (MediatR, Result
  pattern, soft delete, ...). Read it for platform vocabulary; do not write product domain
  terms into it, and do not move its entries into `GLOSSARY.md`.

If a file does not exist, proceed silently; it is created lazily when something is resolved.

## ADR numbering: three digits

This repo numbers ADRs with three digits (`001-module-feature-pattern.md` ... `008-...`), not
the four-digit `0001-` form the skill templates show. The next ADR takes the next free
three-digit number and gets a row in the index of `docs/adr/README.md`. A product decision
can be the short form from `ADR-FORMAT.md`; `docs/adr/template.md` is for pattern-level
decisions like 001–008.

## Use the glossary's vocabulary

When your output names a domain concept (issue title, proposal, test name), use the term as
defined in `GLOSSARY.md`. A concept that is missing is either invented language (reconsider)
or a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, say so explicitly rather than silently overriding:

> _Contradicts ADR-004 (EF Core over repositories), but worth reopening because..._
