#!/usr/bin/env bash
#
# Get total downloads for a NuGet package, counting only listed
# (publicly visible) versions.
#
# Why not api.nuget.org's package TotalDownloads? It sums every version
# ever published, including unlisted ones. This uses NuGet's Search API
# instead, which only ever indexes listed versions and gives per-version
# download counts.
#
# Requires: curl, jq
#
# Usage:
#   ./nuget_listed_downloads.sh Nkraft.MvvmEssentials

set -euo pipefail

PACKAGE_ID="${1:-Nkraft.MvvmEssentials}"
SEARCH_URL="https://azuresearch-usnc.nuget.org/query"

response=$(curl -s -G "$SEARCH_URL" \
  --data-urlencode "q=packageid:${PACKAGE_ID}" \
  --data-urlencode "prerelease=true" \
  --data-urlencode "semVerLevel=2.0.0")

count=$(echo "$response" | jq '.data | length')
if [ "$count" -eq 0 ]; then
  echo "Package '${PACKAGE_ID}' not found (or has no listed versions)." >&2
  exit 1
fi

TOTAL=$(echo "$response" | jq '[.data[0].versions[].downloads] | add')

if [[ "${1:-}" == "--ci" ]] || [[ "${2:-}" == "--ci" ]]; then
  # Machine-readable line, same contract as extract-metrics.sh's coverage=/cc=/crap=
  echo "downloads=$TOTAL"
  exit 0
fi

echo "Package:            $(echo "$response" | jq -r '.data[0].id')"
echo "Listed versions:    $(echo "$response" | jq '.data[0].versions | length')"
echo "Total downloads:    $TOTAL"
echo
echo "Per-version breakdown:"
echo "$response" | jq -r '.data[0].versions | sort_by(.version) | .[] | "  \(.version)  \(.downloads)"'
