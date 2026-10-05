#!/bin/sh
set -eu

# Refuse to use the development credentials in appsettings.json on Render.
for key in ConnectionStrings__DefaultConnection Jwt__Secret InitialAdmin__Email InitialAdmin__Password Planning__BaseUrl Planning__InternalKey Cors__AllowedOrigins__0; do
    value="$(printenv "$key" || true)"
    if [ -z "$value" ]; then
        echo "Required deployment environment variable is missing: $key" >&2
        exit 1
    fi
done

export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-10000}"
exec dotnet BuildFlow.Api.dll
