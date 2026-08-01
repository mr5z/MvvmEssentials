#!/usr/bin/env bash
set -e

COBERTURA="${1:-coverage.cobertura.xml}"
PACKAGE="${2:-Nkraft.MvvmEssentials}"

if [[ ! -f "$COBERTURA" ]]; then
    echo "File not found: $COBERTURA" >&2
    exit 1
fi

# Line coverage %, scoped to $PACKAGE and excluding obj/generated files
# (mirrors reportgenerator's -assemblyfilters + -filefilters:"-**/obj/**").
# Root <coverage line-rate="..."> is the aggregate across ALL packages and
# must NOT be used here — it does not match a filtered report.
COVERAGE=$(awk -v pkg="$PACKAGE" '
    $0 ~ "<package name=\"" pkg "\"" { f=1 }
    f && /<class / { skip = ($0 ~ /filename="[^"]*\/obj\//) ? 1 : 0 }
    f && !skip && /<line number="[0-9]+" hits="[0-9]+"/ {
        valid++
        if (match($0, /hits="[0-9]+"/)) {
            hitstr = substr($0, RSTART, RLENGTH)
            gsub(/hits="|"/, "", hitstr)
            if (hitstr != "0") covered++
        }
    }
    /<\/package>/ { f=0 }
    END { if (valid > 0) printf "%.1f", (covered/valid) * 100 }
' "$COBERTURA")

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
    f && /<class / {
        # Mirror reportgenerator: -filefilters:"-**/obj/**"
        skip = ($0 ~ /filename="[^"]*\/obj\//) ? 1 : 0
    }
    f && /<method / && !skip { print }
    /<\/package>/ { f=0 }
' "$COBERTURA")

BEST_CRAP=$(printf "%.1f" "$BEST_CRAP")

echo "coverage=$COVERAGE"
echo "cc=$BEST_CC"
echo "crap=$BEST_CRAP"
