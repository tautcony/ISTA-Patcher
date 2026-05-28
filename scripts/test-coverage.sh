#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT="$ROOT_DIR/src/ISTestA/ISTestA.csproj"
RUNSETTINGS="$ROOT_DIR/coverage.runsettings"
CONFIGURATION="Debug"
RESULTS_BASE="$ROOT_DIR/TestResults"
GENERATE_HTML=false
OPEN_HTML=false
CLEAN=false

usage() {
  cat <<'EOF'
Usage: scripts/test-coverage.sh [options]

Runs unit tests with XPlat Code Coverage and prints a summary for this run only.

Options:
  -c, --configuration <name>  Build configuration. Default: Debug
  --clean                    Remove TestResults and legacy coverage-report before running
  --html                     Generate coverage-report/index.html inside this run directory
  --open                     Open this run's coverage-report/index.html after generating HTML
  -h, --help                 Show this help

Examples:
  scripts/test-coverage.sh
  scripts/test-coverage.sh --html
  scripts/test-coverage.sh --clean --html --open
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -c|--configuration)
      CONFIGURATION="${2:?missing value for $1}"
      shift 2
      ;;
    --clean)
      CLEAN=true
      shift
      ;;
    --html)
      GENERATE_HTML=true
      shift
      ;;
    --open)
      GENERATE_HTML=true
      OPEN_HTML=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "[ERROR] Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ "$CONFIGURATION" != "Debug" ]]; then
  echo "[WARN] Coverage should normally run with Debug. Release may omit projects with disabled debug symbols."
fi

if [[ ! -f "$TEST_PROJECT" ]]; then
  echo "[ERROR] Test project not found: $TEST_PROJECT" >&2
  exit 1
fi

if [[ ! -f "$RUNSETTINGS" ]]; then
  echo "[ERROR] Runsettings file not found: $RUNSETTINGS" >&2
  exit 1
fi

if [[ "$CLEAN" == true ]]; then
  rm -rf "$RESULTS_BASE" "$ROOT_DIR/coverage-report"
fi

RUN_ID="$(date +%Y%m%d-%H%M%S)"
RESULT_DIR="$RESULTS_BASE/$RUN_ID"
RAW_RESULT_DIR="$RESULT_DIR/.dotnet-test-results"
NORMALIZED_TRX_FILE="$RESULT_DIR/unit-tests.trx"
NORMALIZED_COVERAGE_FILE="$RESULT_DIR/coverage.cobertura.xml"
HTML_REPORT_DIR="$RESULT_DIR/coverage-report"
mkdir -p "$RESULT_DIR"
mkdir -p "$RAW_RESULT_DIR"

echo "[INFO] Results directory: $RESULT_DIR"
echo "[INFO] Running tests with coverage..."

TEST_EXIT_CODE=0
dotnet test "$TEST_PROJECT" \
  --configuration "$CONFIGURATION" \
  --settings "$RUNSETTINGS" \
  --collect:"XPlat Code Coverage" \
  --logger "trx;LogFileName=unit-tests.trx" \
  --results-directory "$RAW_RESULT_DIR" || TEST_EXIT_CODE=$?

COVERAGE_FILE="$(find "$RAW_RESULT_DIR" -name 'coverage.cobertura.xml' -type f | head -n 1)"
TRX_FILE="$(find "$RAW_RESULT_DIR" -name '*.trx' -type f | head -n 1 || true)"

if [[ -n "$TRX_FILE" ]]; then
  cp "$TRX_FILE" "$NORMALIZED_TRX_FILE"
  TRX_FILE="$NORMALIZED_TRX_FILE"
fi

if [[ -z "$COVERAGE_FILE" ]]; then
  rm -rf "$RAW_RESULT_DIR"
  echo "[ERROR] Coverage file was not generated under: $RESULT_DIR" >&2
  exit "$TEST_EXIT_CODE"
fi

cp "$COVERAGE_FILE" "$NORMALIZED_COVERAGE_FILE"
COVERAGE_FILE="$NORMALIZED_COVERAGE_FILE"
rm -rf "$RAW_RESULT_DIR"

echo
echo "[INFO] Test result: ${TRX_FILE:-not found}"
echo "[INFO] Coverage XML: $COVERAGE_FILE"
echo

if command -v python3 >/dev/null 2>&1; then
  python3 - "$COVERAGE_FILE" <<'PY'
import sys
import xml.etree.ElementTree as ET

coverage_file = sys.argv[1]
root = ET.parse(coverage_file).getroot()

def pct(value: str) -> str:
    return f"{float(value) * 100:.2f}%"

print("Coverage summary")
print("================")
print(f"Line rate:   {pct(root.attrib['line-rate'])} ({root.attrib['lines-covered']}/{root.attrib['lines-valid']})")
print(f"Branch rate: {pct(root.attrib['branch-rate'])} ({root.attrib['branches-covered']}/{root.attrib['branches-valid']})")
print()
print("Packages")
print("--------")
for package in root.findall("./packages/package"):
    print(
        f"{package.attrib['name']}: "
        f"lines {pct(package.attrib['line-rate'])}, "
        f"branches {pct(package.attrib['branch-rate'])}"
    )
PY
else
  echo "[WARN] python3 not found; skipping coverage summary."
fi

if [[ "$GENERATE_HTML" == true ]]; then
  if ! command -v reportgenerator >/dev/null 2>&1; then
    echo
    echo "[INFO] reportgenerator not found. Installing dotnet-reportgenerator-globaltool..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
    export PATH="$PATH:$HOME/.dotnet/tools"
  fi

  echo
  echo "[INFO] Generating HTML report..."
  reportgenerator \
    "-reports:$COVERAGE_FILE" \
    "-targetdir:$HTML_REPORT_DIR" \
    "-reporttypes:Html;TextSummary"

  echo "[INFO] HTML report: $HTML_REPORT_DIR/index.html"

  if [[ "$OPEN_HTML" == true ]]; then
    case "$(uname -s)" in
      Darwin)
        open "$HTML_REPORT_DIR/index.html"
        ;;
      Linux)
        xdg-open "$HTML_REPORT_DIR/index.html"
        ;;
      *)
        echo "[WARN] Automatic open is not supported on this OS."
        ;;
    esac
  fi
fi

exit "$TEST_EXIT_CODE"
