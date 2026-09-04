#!/usr/bin/env bash
# Publishes the self-contained Linux binary. Mirrors publish.ps1 (Windows).
set -euo pipefail

RID="${1:-linux-x64}"
OUT="publish/$RID"

cd "$(dirname "$0")"

dotnet publish src/WAVE.App/WAVE.App.csproj \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

echo "Publicado em $OUT/WAVE"
