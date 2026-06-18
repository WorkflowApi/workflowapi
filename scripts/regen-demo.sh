#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

DUMP_DIR="out/dump"
TARGET="src/web/workflow-react-flow-visualizer-mvp/src/data/risk-enrichment.workflowapi.yaml"

echo "Cleaning previous dump..."
rm -rf "$DUMP_DIR"

echo "Running scanner..."
dotnet run --project src/temporal-local/worker -- --workflowapi-dump "$DUMP_DIR"

GENERATED=$(ls "$DUMP_DIR"/*.workflowapi.yaml | head -1)
echo "Installing $GENERATED into visualiser..."
cp "$GENERATED" "$TARGET"

echo "Done. $(wc -l <"$TARGET") lines installed."
echo "Start visualiser: npm --prefix src/web/workflow-react-flow-visualizer-mvp run dev"
