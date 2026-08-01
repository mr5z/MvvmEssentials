#!/usr/bin/env bash
set -e

COBERTURA="${1:-coverage.cobertura.xml}"

if [[ ! -f "$COBERTURA" ]]; then
    echo "File not found: $COBERTURA" >&2
    exit 1
fi

# Overall line coverage %
COVERAGE=$(grep -oP '(?<=<coverage line-rate=")[^"]+' "$COBERTURA" | head -1 | awk '{printf "%.1f", $1 * 100}')

# Per-method complexity + line-rate -> CRAP score
TOP_CC=0
TOP_CRAP=0

while read -r line; do
    CC=$(echo "$line" | grep -oP '(?<=complexity=")[^"]+')
    LR=$(echo "$line" | grep -oP '(?<=line-rate=")[^"]+')

    [[ -z "$CC" || -z "$LR" ]] && continue

    CRAP=$(awk -v cc="$CC" -v lr="$LR" 'BEGIN { printf "%.1f", (cc*cc) * ((1-lr)^3) + cc }')

    if awk -v a="$CC" -v b="$TOP_CC" 'BEGIN{exit !(a>b)}'; then
        TOP_CC="$CC"
    fi
    if awk -v a="$CRAP" -v b="$TOP_CRAP" 'BEGIN{exit !(a>b)}'; then
        TOP_CRAP="$CRAP"
    fi
done < <(grep '<method ' "$COBERTURA")

echo "coverage=$COVERAGE"
echo "cc=$TOP_CC"
echo "crap=$TOP_CRAP"
