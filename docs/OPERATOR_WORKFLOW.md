# Operator Workflow

人間オペレーターの実ワークフロー・痛点・品質目標を保持する正本。

## Overall Flow

1. Assistant scopes the current artifact and checks repo-local rules.
2. Assistant implements or prepares the smallest artifact-moving change.
3. Assistant runs narrow mechanical checks when available.
4. User performs Unity Editor / scene / visual judgement when acceptance depends
   on actual editor behavior or creative feel.
5. Assistant records evidence and syncs only the owning docs.

## Actor Boundaries

- user: final visual/creative judgement, Unity Editor acceptance when the editor
  must be observed, reopening frozen product frontiers.
- assistant: source edits, doc sync, static checks, focused tests, readback, gap
  reports.
- tool: scripted checks, local servers, browser or editor automation when
  available and task-relevant.
- shared: manual Editor verification followed by assistant evidence capture.

## Manual Verification Boundaries

- Unity Editor compile/play/scene behavior is not accepted from prose alone.
- If the assistant cannot run the required Unity verification, it must state the
  missing check and provide the exact user-owned verification target.
- Do not mix manual verification requests with strategic next-direction choices.

## Pain Points to Avoid

- Rebuilding context from long historical handoffs when `runtime-state` and the
  owning spec are enough.
- Treating stale March-era progress summaries as current acceptance.
- Spreading the same rule across `AGENTS.md`, `CLAUDE.md`, docs, and tool config.
- Asking the user to choose between commit/no-commit instead of explaining the
  actual bottleneck.

## Quality Goal

The workflow is healthy when a new agent can read the normal 3-file restart set,
identify the current artifact and bottleneck, and know which doc owns any deeper
evidence it needs.
