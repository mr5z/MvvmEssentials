#!/usr/bin/env bash
set -e

cd ..

dotnet test --collect:"XPlat Code Coverage"

LATEST=$(find tests/TestResults -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' | sort -nr | head -1 | cut -d' ' -f2-)

reportgenerator \
    -reports:"$LATEST/coverage.cobertura.xml" \
    -targetdir:coveragereport \
    -reporttypes:Html \
    -assemblyfilters:"+Nkraft.MvvmEssentials" \
    -filefilters:"-**/obj/**"

