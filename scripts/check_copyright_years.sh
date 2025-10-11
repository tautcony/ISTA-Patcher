#!/bin/bash
# Script to check if copyright years in file headers match git history

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Checking copyright years in file headers against git history..."
echo "================================================================"
echo

# Find all C# files
find src -name "*.cs" -type f | while read -r file; do
    # Convert to absolute path
    abs_file=$(realpath "$file")

    # Get the first and last commit years for this file
    first_year=$(git log --follow --find-renames --format=%ad --date=format:%Y "$file" 2>/dev/null | tail -1)
    last_year=$(git log --follow --find-renames --format=%ad --date=format:%Y "$file" 2>/dev/null | head -1)

    # Skip if file is not in git history
    if [ -z "$first_year" ]; then
        continue
    fi

    # Extract copyright years from file header
    header_line=$(head -5 "$file" | grep -i "SPDX-FileCopyrightText\|Copyright")

    if [ -z "$header_line" ]; then
        echo -e "${RED}[MISSING]${NC} $abs_file - No copyright header found"
        continue
    fi

    # Extract year range from header (handles both "YYYY" and "YYYY-YYYY" formats)
    if echo "$header_line" | grep -qE "Copyright [0-9]{4}-[0-9]{4}"; then
        header_first_year=$(echo "$header_line" | grep -oE "[0-9]{4}-[0-9]{4}" | cut -d'-' -f1)
        header_last_year=$(echo "$header_line" | grep -oE "[0-9]{4}-[0-9]{4}" | cut -d'-' -f2)
    elif echo "$header_line" | grep -qE "Copyright [0-9]{4}"; then
        header_first_year=$(echo "$header_line" | grep -oE "Copyright [0-9]{4}" | grep -oE "[0-9]{4}")
        header_last_year="$header_first_year"
    else
        echo -e "${RED}[ERROR]${NC} $abs_file - Cannot parse copyright years from: $header_line"
        continue
    fi

    # Compare years
    status="OK"
    message=""
    is_warning=false

    # Check first year - if header year is less than git year, it's just a warning
    if [ "$first_year" != "$header_first_year" ]; then
        if [ "$header_first_year" -lt "$first_year" ]; then
            status="WARNING"
            is_warning=true
            message="First year in header ($header_first_year) is earlier than git history ($first_year)"
        else
            status="MISMATCH"
            message="First year mismatch: git=$first_year, header=$header_first_year"
        fi
    fi

    # Check last year
    if [ "$last_year" != "$header_last_year" ]; then
        if [ -n "$message" ]; then
            message="$message; "
        fi
        if [ "$status" != "MISMATCH" ]; then
            status="MISMATCH"
        fi
        message="${message}Last year mismatch: git=$last_year, header=$header_last_year"
    fi

    if [ "$status" = "OK" ]; then
        echo -e "${GREEN}[OK]${NC} $abs_file - $header_first_year-$header_last_year"
    elif [ "$status" = "WARNING" ]; then
        echo -e "${YELLOW}[WARNING]${NC} $abs_file"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Header:      $header_first_year-$header_last_year"
        echo -e "  ${YELLOW}Note: $message${NC}"
    else
        echo -e "${RED}[MISMATCH]${NC} $abs_file"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Header:      $header_first_year-$header_last_year"
        echo -e "  Details: $message"
    fi
done

echo
echo "================================================================"
echo "Check complete!"
