#!/usr/bin/env bash
# Ejecuta las pruebas EditMode de Unity en modo batch y resume el resultado.
# Uso: scripts/run-unity-tests.sh [ruta-del-log]
set -u
UNITY="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOG="${1:-$ROOT/TestResults/unity-editmode.log}"
mkdir -p "$ROOT/TestResults"
rm -f "$ROOT/TestResults/editmode.xml"
"$UNITY" -batchmode -nographics -projectPath "$ROOT/unity/Flagstorm" -runTests -testPlatform EditMode \
  -testResults "$ROOT/TestResults/editmode.xml" -logFile "$LOG"
code=$?
grep -E "error CS[0-9]+" "$LOG" | sed 's/^.*Assets/Assets/' | sort -u | head -20
if [ -f "$ROOT/TestResults/editmode.xml" ]; then
  grep -oE '<test-run [^>]*' "$ROOT/TestResults/editmode.xml" | grep -oE '(result|total|passed|failed)="[^"]*"' | tr '\n' ' '
  echo
  grep -oE '<test-case [^>]*result="Failed"[^>]*' "$ROOT/TestResults/editmode.xml" | grep -oE 'name="[^"]*"'
fi
echo "unity exit $code"
exit $code
