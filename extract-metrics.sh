#!/usr/bin/env bash
set -e

COBERTURA="${1:-coverage.cobertura.xml}"
PACKAGE="${2:-Nkraft.MvvmEssentials}"

if [[ ! -f "$COBERTURA" ]]; then
    echo "File not found: $COBERTURA" >&2
    exit 1
fi

# Which classes count: only those compiled from a real source file.
#
# Primary filter is upstream: CI runs coverlet with
# ExcludeByAttribute=GeneratedCodeAttribute, which drops well-behaved
# source-generator output (MAUI bindings, CommunityToolkit.Mvvm, ...).
#
# This is the backstop for generators that don't mark their code. A class is
# skipped when its filename:
#   - has an obj/ path segment (emitted generator/XAML output), or
#   - does not exist on disk (in-memory generator output has a virtual path).
#     Relative filenames are resolved against the report's <source> roots.
# Run this in the same checkout the tests ran in, or every class is skipped
# (guarded below).
FILTER='
    # Coverlet writes class filenames RELATIVE to the <source> root(s)
    # (the common directory of all instrumented sources), so resolve them.
    /<source>/ {
        src = $0; sub(/.*<source>/, "", src); sub(/<\/source>.*/, "", src)
        if (src !~ /\/$/) src = src "/"
        roots[++nroots] = src
    }
    function exists(p,   l, r) { r = ((getline l < p) >= 0); close(p); return r }
    function is_real_source(line,   fn, i, ok) {
        if (!match(line, /filename="[^"]*"/)) return 0
        fn = substr(line, RSTART + 10, RLENGTH - 11)
        if (fn ~ /(^|\/)obj\//) return 0
        if (fn in seen) return seen[fn]
        ok = 0
        if (fn ~ /^\//) ok = exists(fn)
        else for (i = 1; i <= nroots && !ok; i++) ok = exists(roots[i] fn)
        return seen[fn] = ok
    }
    $0 ~ "<package name=\"" pkg "\"" { f=1 }
    /<\/package>/ { f=0 }
    f && /<class / { skip = !is_real_source($0) }
'

# Line coverage %, scoped to $PACKAGE and the classes above.
# Root <coverage line-rate="..."> is the aggregate across ALL packages and
# must NOT be used here — it does not match a filtered report.
COVERAGE=$(awk -v pkg="$PACKAGE" "$FILTER"'
    f && !skip && /<line number="[0-9]+" hits="[0-9]+"/ {
        valid++
        if (match($0, /hits="[0-9]+"/)) {
            hitstr = substr($0, RSTART, RLENGTH)
            gsub(/hits="|"/, "", hitstr)
            if (hitstr != "0") covered++
        }
    }
    END { if (valid > 0) printf "%.1f", (covered/valid) * 100 }
' "$COBERTURA")

if [[ -z "$COVERAGE" ]]; then
    echo "No lines counted for package '$PACKAGE'. Wrong package name, or source" >&2
    echo "paths in $COBERTURA don't exist here (run from the test checkout)." >&2
    exit 1
fi

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
done < <(awk -v pkg="$PACKAGE" "$FILTER"'
    f && !skip && /<method / { print }
' "$COBERTURA")

BEST_CRAP=$(printf "%.1f" "$BEST_CRAP")

echo "coverage=$COVERAGE"
echo "cc=$BEST_CC"
echo "crap=$BEST_CRAP"