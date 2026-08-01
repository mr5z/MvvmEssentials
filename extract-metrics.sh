#!/usr/bin/env bash
set -e

COBERTURA="${1:-coverage.cobertura.xml}"
PACKAGE="${2:-Nkraft.MvvmEssentials}"

if [[ ! -f "$COBERTURA" ]]; then
    echo "File not found: $COBERTURA" >&2
    exit 1
fi

# Overall line coverage % (root <coverage> aggregate)
COVERAGE=$(grep -oP '(?<=<coverage line-rate=")[^"]+' "$COBERTURA" | head -1 | awk '{printf "%.1f", $1 * 100}')

# Single method with the highest CRAP score, scoped to $PACKAGE only.
# BEST_CC and BEST_CRAP always come from the SAME method — never mixed.
BEST_CRAP=0
BEST_CC=0

while read -r line; do
    CC=$(echo "$line" | grep -oP '(?<=complexity=")[^"]+')
    LR=$(echo "$line" | grep -oP '(?<=line-rate=")[^"]+')

    [[ -z "$CC" || -z "$LR" ]] && continue

    CRAP=$(awk -v cc="$CC" -v lr="$LR" 'BEGIN { printf "%.4f", (cc*cc) * ((1-lr)^3) + cc }')

    if awk -v a="$CRAP" -v b="$BEST_CRAP" 'BEGIN{exit !(a>b)}'; then
        BEST_CRAP="$CRAP"
        BEST_CC="$CC"
    fi
done < <(awk -v pkg="$PACKAGE" '
    $0 ~ "<package name=\"" pkg "\"" { f=1 }
    f && /<method / { print }
    /<\/package>/ { f=0 }
' "$COBERTURA")

BEST_CRAP=$(printf "%.1f" "$BEST_CRAP")

echo "coverage=$COVERAGE"
echo "cc=$BEST_CC"
echo "crap=$BEST_CRAP"
