# ADR 001: Structured LLM proposal inside the existing planning workflow

**Status:** Accepted for Member 01

## Context

Member 01 already exposes a version 1.0 `POST /internal/plans` contract to ASP.NET Core and defines three downstream tasks with fixed agent-result contracts and workflow states. Planning should account for construction context without allowing generated text to authorize actions or change these contracts.

## Decision

- **FastAPI** hosts the private asynchronous endpoint consumed only by ASP.NET Core. React and Flutter continue to call ASP.NET Core.
- **Pydantic v2** validates the incoming request and a strict Gemini proposal schema (`extra=forbid`, typed agent names, actions, step kinds, lengths and quantities). A separate accepted-plan model describes the stable wire response.
- **Gemini `gemini-3.5-flash-lite`** supplies a structured proposal using JSON response mode and the Pydantic schema. The API key stays in the Python server environment. Model response formatting is guidance; the application independently parses and validates every response.
- **LangGraph `StateGraph`** represents model call, bounded retry, validation, materialization, and safe-failure transitions. Node events provide an execution trace without storing prompts or private reasoning.
- **Deterministic control** checks request-derived counts, deadline, budget and priority; exact step order; exact allow-listed agent/action/dependency sequence; approval; and prohibited high-impact or unverified claims. Trusted code alone creates UUID task IDs, immutable task inputs, dependency IDs, and the `AwaitingAgents` state.

## Agent boundary and permissions

The Planning Agent receives only validated context supplied by ASP.NET Core. It does not query PostgreSQL or invoke arbitrary tools. The Gemini call is configured without tools. The only proposed delegate agents are `InventoryAnalysisAgent`, `ProcurementAgent`, and `SchedulingValidationAgent`; the first maps to the established wire alias `InventoryAgent`. This alias preserves existing Member 02–04 inputs, outputs, dependency ordering and the ASP.NET `agent-results` endpoint. No downstream agent's internal logic changes. Procurement, reservation, assignment, approval, and other high-impact operations remain paused for deterministic backend validation and authorized human approval.

## Failure, audit, and compatibility

Malformed or unsupported proposals fail closed before task materialization. Gemini API errors and timeouts receive bounded retries; exhausted failures return a safe error category. Successful and failed attempts carry an execution summary: agent name, workflow ID, accepted generated plan when available, schema and business validation results, timing, retry count, categorized errors, node events, and final planning status. ASP.NET Core stores this in its existing workflow JSON column, preserving previous attempt summaries on a retry. No new database table or migration is needed. The existing `Queued → AwaitingAgents → AwaitingBackendValidation → PendingProjectManagerApproval → Approved|Rejected|RevisionRequested` progression and `Failed` branch are preserved. The current Member 01 backend stops at `AwaitingBackendValidation`; later validation and approval integrations remain separate.

## Consequences

Planning depends on Gemini service availability and a valid server-side key. When unavailable, the workflow fails safely with a retryable audit trail. Model proposals can improve descriptive planning, but cannot change delegated actions, execute them, or bypass manager approval. Tests inject a fake gateway to verify contracts and failure behavior without external calls.
