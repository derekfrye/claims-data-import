#!/usr/bin/env zsh
set -euo pipefail

# build_and_analyze_style.sh
# - Cleans and builds a .NET solution
# - Saves logs to build_artifacts
# - Parses analyzer diagnostics to summarize top rules and per-project breakdowns

usage() {
  cat <<'USAGE'
Usage: build_and_analyze_style.sh [-s <solution.sln>] [-p <project.csproj>] [-w]

Options:
  -s <solution.sln>   Solution file to build (default: ClaimsDataImport.sln)
  -p <project.csproj> Project filter for per-project rule summary (optional)
  -w                  Build with -warnaserror (optional)

Outputs are written to the build_artifacts/ directory with timestamped logs
and convenience files (clean.log, build.log, summaries).
USAGE
}

sln="ClaimsDataImport.sln"
proj=""
warnaserror=0

while getopts ":s:p:wh" opt; do
  case ${opt} in
    s) sln="${OPTARG}" ;;
    p) proj="${OPTARG}" ;;
    w) warnaserror=1 ;;
    h) usage; exit 0 ;;
    \?) echo "Unknown option: -${OPTARG}" >&2; usage; exit 2 ;;
    :) echo "Option -${OPTARG} requires an argument" >&2; usage; exit 2 ;;
  esac
done
shift $((OPTIND-1))

ARTIFACT_DIR="build_artifacts"
mkdir -p "${ARTIFACT_DIR}"

ts=$(date +%Y%m%d_%H%M%S)
CLEAN_LOG="${ARTIFACT_DIR}/clean_${ts}.log"
BUILD_LOG="${ARTIFACT_DIR}/build_${ts}.log"

echo "== dotnet clean ==" | tee "${CLEAN_LOG}" >/dev/null
DOTNET_CLI_UI_LANGUAGE=en dotnet clean "${sln}" 2>&1 | tee -a "${CLEAN_LOG}"

echo "\n== dotnet build ==" | tee "${BUILD_LOG}" >/dev/null
build_cmd=(dotnet build "${sln}")
if (( warnaserror == 1 )); then
  build_cmd+=(-warnaserror)
fi
DOTNET_CLI_UI_LANGUAGE=en "${build_cmd[@]}" 2>&1 | tee -a "${BUILD_LOG}" || true

# Helper to create symlinks using absolute targets and only if the target exists
safe_link() {
  local target="$1"
  local linkpath="$2"
  if [[ -f "$target" ]]; then
    local abspath
    abspath=$(readlink -f "$target" 2>/dev/null || echo "$target")
    ln -sf "$abspath" "$linkpath"
  else
    echo "[warn] Not linking $linkpath -> $target (target missing)" >&2
  fi
}

# Update convenient latest pointers (use absolute targets)
safe_link "${CLEAN_LOG}" "${ARTIFACT_DIR}/clean.log"
safe_link "${BUILD_LOG}" "${ARTIFACT_DIR}/build.log"

# Helpers for parsing with ripgrep if available, else grep/awk fallback
have_rg=0
if command -v rg >/dev/null 2>&1; then
  have_rg=1
fi

rules_all="${ARTIFACT_DIR}/warnings_by_id_${ts}.txt"
projects_all="${ARTIFACT_DIR}/warnings_by_project_${ts}.txt"
rules_by_project="${ARTIFACT_DIR}/warnings_by_id_for_project_${ts}.txt"
rules_all_latest="${ARTIFACT_DIR}/warnings_by_id.txt"
projects_all_latest="${ARTIFACT_DIR}/warnings_by_project.txt"
rules_by_project_latest="${ARTIFACT_DIR}/warnings_by_id_for_project.txt"
samples_file="${ARTIFACT_DIR}/diagnostics_samples_${ts}.txt"

# Capture some samples (always create file)
if (( have_rg )); then
  rg -N "(warning|error)\s+[A-Z]{2}\d{3,4}" "${BUILD_LOG}" | head -n 40 > "${samples_file}" || true
else
  grep -E "(warning|error) [A-Z]{2}[0-9]{3,4}" "${BUILD_LOG}" | head -n 40 > "${samples_file}" || true
fi
touch "${samples_file}"
safe_link "${samples_file}" "${ARTIFACT_DIR}/diagnostics_samples.txt"

# Top rules across the solution (always create file)
if (( have_rg )); then
  rg -N -o -e '(warning|error)\s+([A-Z]{2}\d{3,4})' --replace '$2' "${BUILD_LOG}" \
    | sort | uniq -c | sort -nr > "${rules_all}" || true
else
  grep -Eo "(warning|error) [A-Z]{2}[0-9]{3,4}" "${BUILD_LOG}" \
    | awk '{print $2}' | sort | uniq -c | sort -nr > "${rules_all}" || true
fi
touch "${rules_all}"
safe_link "${rules_all}" "${rules_all_latest}"

# Top projects by diagnostics (always create file)
if (( have_rg )); then
  rg -N -o -e '(warning|error)\s+[A-Z]{2}\d{3,4}.*\[([^\]]+\.csproj)\]' --replace '$2' "${BUILD_LOG}" \
    | xargs -r -n1 basename | sort | uniq -c | sort -nr > "${projects_all}" || true
else
  # Fallback: best-effort extract content in brackets ending with .csproj
  grep -E "(warning|error) [A-Z]{2}[0-9]{3,4}.*\[[^]]+\.csproj\]" "${BUILD_LOG}" \
    | sed -E 's/.*\[([^]]+\.csproj)\].*/\1/' | xargs -r -n1 basename \
    | sort | uniq -c | sort -nr > "${projects_all}" || true
fi
touch "${projects_all}"
safe_link "${projects_all}" "${projects_all_latest}"

# If a project is passed, compute top rules for that project
if [[ -n "${proj}" ]]; then
  proj_name=$(basename -- "${proj}")
  if (( have_rg )); then
    rg -N -o -e "(warning|error)\s+([A-Z]{2}\\d{3,4}).*\\[[^]]*${proj_name}\\]" --replace '$2' "${BUILD_LOG}" \
      | sort | uniq -c | sort -nr > "${rules_by_project}" || true
  else
    grep -E "(warning|error) [A-Z]{2}[0-9]{3,4}.*\[[^]]*${proj_name}\]" "${BUILD_LOG}" \
      | sed -E 's/.*(warning|error) ([A-Z]{2}[0-9]{3,4}).*/\2/' \
      | sort | uniq -c | sort -nr > "${rules_by_project}" || true
  fi
  touch "${rules_by_project}"
  safe_link "${rules_by_project}" "${rules_by_project_latest}"
fi

# Console summary
echo
echo "Top rules (all projects):"
head -n 10 "${rules_all}" || true

echo
echo "Top projects by diagnostics:"
head -n 10 "${projects_all}" || true

if [[ -n "${proj}" ]]; then
  echo
  echo "Top rules for project: ${proj}"
  head -n 15 "${rules_by_project}" || true
fi

echo
# Resolved paths to avoid transient lookup pain
res_clean=$(readlink -f "${ARTIFACT_DIR}/clean.log" 2>/dev/null || echo "${CLEAN_LOG}")
res_build=$(readlink -f "${ARTIFACT_DIR}/build.log" 2>/dev/null || echo "${BUILD_LOG}")
res_rules=$(readlink -f "${ARTIFACT_DIR}/warnings_by_id.txt" 2>/dev/null || echo "${rules_all}")
res_projects=$(readlink -f "${ARTIFACT_DIR}/warnings_by_project.txt" 2>/dev/null || echo "${projects_all}")
res_rules_proj=""
if [[ -n "${proj}" ]]; then
  res_rules_proj=$(readlink -f "${ARTIFACT_DIR}/warnings_by_id_for_project.txt" 2>/dev/null || echo "${rules_by_project}")
fi
echo "Logs (resolved): ${res_clean} | ${res_build}"
echo "Summaries (resolved): ${res_rules} | ${res_projects}${proj:+ | ${res_rules_proj}}"
