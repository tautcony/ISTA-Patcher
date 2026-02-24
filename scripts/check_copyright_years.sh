#!/bin/bash
# Script to check if copyright years in file headers match git history

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

base_dir="${1:-$PWD}"
base_dir=$(realpath "$base_dir")
src_dir="$base_dir/src"

if [ ! -d "$src_dir" ]; then
    echo -e "${RED}[ERROR]${NC} src directory not found: $src_dir"
    exit 1
fi

if ! git -C "$base_dir" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo -e "${RED}[ERROR]${NC} not a git repository: $base_dir"
    exit 1
fi

echo "Checking copyright years in file headers against git history..."
echo "================================================================"
echo

# Find all C# files
find "$src_dir" -name "*.cs" -type f | while read -r file; do
    # Convert to absolute path
    abs_file=$(realpath "$file")

    # Get the first and last commit years for this file
    rel_file="${file#"$base_dir"/}"
    display_path="$rel_file:1"
    if ! git -C "$base_dir" ls-files --error-unmatch "$rel_file" >/dev/null 2>&1; then
        if git -C "$base_dir" ls-files --others --exclude-standard -- "$rel_file" | grep -q .; then
            echo -e "${YELLOW}[UNTRACKED]${NC} $display_path"
        fi
        continue
    fi
    first_year=$(git -C "$base_dir" log --follow --find-renames --format=%ad --date=format:%Y "$rel_file" 2>/dev/null | tail -1)
    last_year=$(git -C "$base_dir" log --follow --find-renames --format=%ad --date=format:%Y "$rel_file" 2>/dev/null | head -1)

    # Skip if file is not in git history
    if [ -z "$first_year" ]; then
        continue
    fi

    # Extract copyright years from file header
    header_match=$(head -5 "$file" | grep -in "SPDX-FileCopyrightText" | head -1)
    if [ -z "$header_match" ]; then
        header_match=$(head -5 "$file" | grep -in "Copyright" | head -1)
    fi

    if [ -n "$header_match" ]; then
        header_line_number=$(echo "$header_match" | cut -d':' -f1)
        header_line=$(echo "$header_match" | cut -d':' -f2-)
        display_path="$rel_file:$header_line_number"
    fi

    if [ -z "$header_line" ]; then
        echo -e "${RED}[MISSING]${NC} $display_path - No copyright header found"
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
        echo -e "${RED}[ERROR]${NC} $display_path - Cannot parse copyright years from: $header_line"
        continue
    fi

    # Compare years
    status="OK"
    message=""
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
        echo -e "${GREEN}[OK]${NC} $display_path - $header_first_year-$header_last_year"
    elif [ "$status" = "WARNING" ]; then
        echo -e "${YELLOW}[WARNING]${NC} $display_path"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Header:      $header_first_year-$header_last_year"
        echo -e "  ${YELLOW}Note: $message${NC}"
    else
        echo -e "${RED}[MISMATCH]${NC} $display_path"
        echo -e "  Git history: $first_year-$last_year"
        echo -e "  Header:      $header_first_year-$header_last_year"
        echo -e "  Details: $message"
    fi
done

echo
echo "================================================================"
echo "Check complete!"
