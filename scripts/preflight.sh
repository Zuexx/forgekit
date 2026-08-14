#!/usr/bin/env bash
# Reports whether this repository's documented AI workflow is operational on this machine.
# Every check runs before exiting, so one failure does not hide the rest — hence no `set -e`.
#
# The governing rule for every check below: a check that cannot measure its subject must FAIL,
# never pass. An `ok` has to mean "I looked and it was fine", not "I found nothing to look at".
set -uo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
[ -n "$ROOT_DIR" ] && [ -d "$ROOT_DIR" ] || { echo "cannot resolve repository root" >&2; exit 1; }
BIN_DIR="$ROOT_DIR/node_modules/.bin"
FAILED=0

pass() { printf '  ok    %s\n' "$1"; }
fail() { printf '  FAIL  %s\n        fix: %s\n' "$1" "$2"; FAILED=1; }

# BSD stat and GNU stat spell mtime differently. Decide once rather than relying on one
# form failing quietly into the other — GNU's `-f` means --file-system and prints output
# of its own, which a fallback chain would silently mix into the result.
if stat -f %m "$ROOT_DIR" >/dev/null 2>&1; then
  STAT_MTIME=(-f %m)
else
  STAT_MTIME=(-c %Y)
fi

echo "==> Declared tools"
for tool in openspec codegraph grillme; do
  if [ -x "$BIN_DIR/$tool" ]; then
    pass "$tool"
  else
    fail "$tool is not installed" "pnpm install"
  fi
done

echo "==> Workflow configuration"
if [ ! -x "$BIN_DIR/openspec" ]; then
  fail "cannot read workflow configuration" "pnpm install"
else
  probe="preflight-probe-$$"
  probe_dir="$ROOT_DIR/openspec/changes/$probe"
  trap 'rm -rf "$probe_dir"' EXIT
  if "$BIN_DIR/openspec" new change "$probe" >/dev/null 2>&1; then
    rules=$("$BIN_DIR/openspec" instructions proposal --change "$probe" --json 2>/dev/null \
      | node -pe 'JSON.parse(require("fs").readFileSync(0,"utf8")).rules?.length ?? 0' 2>/dev/null || echo 0)
    guidance=$("$BIN_DIR/openspec" instructions apply --change "$probe" --json 2>/dev/null \
      | node -pe 'JSON.parse(require("fs").readFileSync(0,"utf8")).operationGuidance?.length ?? 0' 2>/dev/null || echo 0)
    rm -rf "$probe_dir"
    if [ "$rules" -gt 0 ]; then
      pass "proposal rules readable ($rules)"
    else
      fail "openspec/config.yaml yields no proposal rules" \
           "check rules: in openspec/config.yaml — a plain scalar containing ': ' silently empties the block"
    fi
    if [ "$guidance" -gt 0 ]; then
      pass "apply guidance readable ($guidance)"
    else
      fail "openspec/config.yaml yields no apply guidance" \
           "check operations.apply.guidance in openspec/config.yaml"
    fi
  else
    fail "openspec could not create a probe change" "run: $BIN_DIR/openspec new change probe"
  fi
fi

echo "==> CodeGraph index"
INDEX="$ROOT_DIR/.codegraph/codegraph.db"
if [ ! -f "$INDEX" ]; then
  # A project that has never been indexed needs `init`; `index` refuses with
  # "CodeGraph not initialized". This is the branch a freshly generated product lands on,
  # so naming the wrong command here would misdirect exactly the reader who needs it.
  fail "no CodeGraph index" "pnpm exec codegraph init"
else
  # Compare against the source the index describes, not against HEAD. Committing modifies no
  # source file, so anchoring on commit time would report a correct index as stale after every
  # commit — and a check that cries wolf is one people learn to skip.
  #
  # Known limit: deleting a source file changes no mtime, so a deletion alone does not mark
  # the index stale.
  index_epoch=$(stat "${STAT_MTIME[@]}" "$INDEX" 2>/dev/null)
  newest_src=$(git -C "$ROOT_DIR" ls-files -z -- \
      '*.cs' '*.ts' '*.tsx' '*.js' '*.jsx' '*.mjs' '*.cjs' '*.mts' '*.cts' 2>/dev/null \
    | xargs -0 stat "${STAT_MTIME[@]}" 2>/dev/null | sort -rn | head -1)
  if [ -z "$index_epoch" ] || [ -z "$newest_src" ]; then
    # Enumeration produced nothing — no git, no stat, or no source files. Whatever the cause,
    # the freshness of the index is unknown, and unknown is not ok.
    fail "cannot determine whether the index is current" \
         "check that git and stat work here, then: pnpm exec codegraph index"
  elif [ "$index_epoch" -lt "$newest_src" ]; then
    fail "index is older than the newest source file — impact analysis from it would be out of date" \
         "pnpm exec codegraph index"
  else
    pass "index present and reflects current source"
  fi
fi

echo "==> Git hooks"
hooks_path=$(git -C "$ROOT_DIR" config --get core.hooksPath || true)
hooks_abs=""
[ -n "$hooks_path" ] && hooks_abs=$(cd "$ROOT_DIR" && cd "$hooks_path" 2>/dev/null && pwd)
expected_abs=$(cd "$ROOT_DIR/.githooks" 2>/dev/null && pwd)
if [ -z "$hooks_abs" ] || [ "$hooks_abs" != "$expected_abs" ]; then
  fail "repository hooks are not enabled" "git config core.hooksPath .githooks"
else
  pass "core.hooksPath -> .githooks"
  # Git ignores a non-executable hook without saying so. `dotnet new` does not carry the
  # executable bit, so in a generated product every hook arrives unable to fire, silently.
  for hook in "$ROOT_DIR/.githooks"/*; do
    [ -f "$hook" ] || continue
    if [ -x "$hook" ]; then
      pass "$(basename "$hook") is executable"
    else
      fail "$(basename "$hook") is not executable — git will ignore it without reporting anything" \
           "chmod +x .githooks/$(basename "$hook")"
    fi
  done
fi

echo "==> Capabilities cited by workflow instructions"
# Sort by version and take the newest, so a stale cached plugin version is not what gets
# validated against.
PLUGIN_SKILLS=$(ls -d "$HOME"/.claude/plugins/cache/*/superpowers/*/skills 2>/dev/null | sort -V | tail -1)

if [ -z "$PLUGIN_SKILLS" ]; then
  fail "Superpowers plugin is not installed" \
       "open Claude Code in this repository and approve the plugin declared in .claude/settings.json"
fi

INSTRUCTION_FILES=("$ROOT_DIR/AGENTS.md" "$ROOT_DIR/openspec/config.yaml")

# Every backticked token in EVERY cell of every markdown table row, plus structured literals
# and document paths cited anywhere in either instruction file.
#
# Reading every cell rather than a fixed column is deliberate: fixing on one column skips a
# capability cited in another, which is the same "passes because it never looked" hole this
# check exists to close. An unrecognised token fails rather than being ignored, because the
# defect this was written for -- a stale `grilling` -- carries no prefix a pattern could match.
cited=$(
  {
    awk -F'|' '/^[[:space:]]*\|/ { for (i = 2; i <= NF; i++) print $i }' "$ROOT_DIR/AGENTS.md"
    grep -ohE '`(superpowers:[a-z-]+|/opsx:[a-z]+|grillme|codegraph_explore)`' "${INSTRUCTION_FILES[@]}"
    grep -ohE '`(docs/[A-Za-z0-9._/-]*|[A-Za-z0-9._/-]+\.md)`' "${INSTRUCTION_FILES[@]}"
  } | grep -oE '`[^`]+`' | tr -d '`' | sort -u
)

while IFS= read -r cap; do
  [ -z "$cap" ] && continue
  case "$cap" in
    superpowers:*)
      if [ -z "$PLUGIN_SKILLS" ]; then
        : # already reported once, as its own cause
      elif [ -d "$PLUGIN_SKILLS/${cap#superpowers:}" ]; then
        pass "$cap"
      else
        fail "$cap does not resolve" "correct the skill name in AGENTS.md or openspec/config.yaml"
      fi
      ;;
    /opsx:*)
      if [ -f "$ROOT_DIR/.claude/commands/opsx/${cap#/opsx:}.md" ]; then
        pass "$cap"
      else
        fail "$cap does not resolve" "correct the command name in AGENTS.md"
      fi
      ;;
    /code-review)
      pass "$cap (built-in)"
      ;;
    codegraph_explore)
      if [ -f "$ROOT_DIR/.mcp.json" ] && grep -q '"codegraph"' "$ROOT_DIR/.mcp.json"; then
        pass "$cap"
      else
        fail "$cap has no declared MCP server" "declare codegraph in .mcp.json"
      fi
      ;;
    grillme)
      if [ -x "$BIN_DIR/grillme" ]; then
        pass "$cap"
      else
        fail "$cap does not resolve" "pnpm install"
      fi
      ;;
    "pnpm "*|"npm "*)
      # A cited package script rots the same way a skill name does — but only a script can.
      # `pnpm install` and `pnpm exec …` are built-in subcommands with nothing to resolve, and
      # the frontend's scripts live in app/package.json, not the workflow package.
      sub="${cap#* }"
      first="${sub%% *}"
      case "$first" in
        install|exec|dlx|add|remove|run|update|why|store|approve-builds)
          pass "$cap (pnpm subcommand)"
          ;;
        *)
          if node -e 'const f=process.argv[1],n=process.argv[2];
                      const has=p=>{try{return !!(require(p).scripts||{})[n]}catch(e){return false}};
                      process.exit(has(f)||has(process.argv[3])?0:1)' \
               "$ROOT_DIR/package.json" "$first" "$ROOT_DIR/app/package.json" 2>/dev/null; then
            pass "$cap"
          else
            fail "$cap names no script in package.json or app/package.json" \
                 "add the script, or correct the citation"
          fi
          ;;
      esac
      ;;
    */*|*.md)
      # A document pointer rots the same way a stale skill name does.
      if [ -e "$ROOT_DIR/$cap" ]; then
        pass "$cap"
      else
        fail "$cap does not exist" "correct the path in AGENTS.md, or restore the document"
      fi
      ;;
    *[![:lower:][:digit:]-]*)
      # Not shaped like a capability name — a type, an identifier, a phrase. Documentation
      # legitimately backticks these, and failing on them would make preflight red on ordinary
      # doc edits, which is the other way a check gets ignored. Say nothing.
      #
      # POSIX classes, not `[!a-z0-9-]`: under en_US.UTF-8 collation the a-z range spans
      # aAbBcC..., so uppercase falls INSIDE it and `ISoftDelete` reached the fail arm.
      :
      ;;
    *)
      # An all-lowercase bare word IS the shape of a capability citation, and is exactly the
      # shape of the stale `grilling` this check was written for. Nothing resolves it.
      fail "unrecognised capability \`$cap\`" \
           "it resolves to nothing — correct it in AGENTS.md, or add it to the toolchain"
      ;;
  esac
done < <(printf '%s\n' "$cited")

echo
if [ "$FAILED" -eq 0 ]; then
  echo "Workflow is operational."
else
  echo "Workflow is not operational. Fix the items above and re-run." >&2
fi
exit "$FAILED"
