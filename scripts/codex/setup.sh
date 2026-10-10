#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
command -v git >/dev/null
command -v node >/dev/null
node scripts/codex/check.mjs
printf '%s\n' 'Cloud source environment ready. Unity Editor import, C# compilation and play tests require a licensed Editor.'
