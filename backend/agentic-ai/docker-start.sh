#!/bin/sh
set -eu
: "${BUILDFLOW_INTERNAL_KEY:?Set BUILDFLOW_INTERNAL_KEY in Render}"
: "${GEMINI_API_KEY:?Set GEMINI_API_KEY in Render}"
case "$BUILDFLOW_INTERNAL_KEY" in
    replace-with-*) echo "Configure a real internal key before deploying." >&2; exit 1 ;;
esac
exec uvicorn main:app --host 0.0.0.0 --port "${PORT:-10000}" --no-access-log
