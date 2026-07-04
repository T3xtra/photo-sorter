#!/usr/bin/env bash
# Publishes PhotoSorter.App as a self-contained build for one Runtime Identifier (RID) and
# zips it into a directly runnable package. Used both by `make package`/`make package-all`
# (local trial builds) and by .github/workflows/release.yml for the osx-*/linux-* matrix
# entries (win-x64 is packaged separately in the workflow via PowerShell's Compress-Archive,
# since windows-latest runners have no reliable `zip` - see docs/architecture-decisions.md).
set -euo pipefail

RID="${1:?Usage: scripts/package.sh <rid> [output-dir]  (rid: osx-x64, osx-arm64, linux-x64, win-x64)}"
OUT_DIR="${2:-dist}"
CONFIGURATION="${CONFIGURATION:-Release}"
APP_NAME="PhotoSorter"
EXECUTABLE_NAME="PhotoSorter.App"
PROJECT="src/PhotoSorter.App/PhotoSorter.App.csproj"

# Resolved independently of the caller's PATH (Make, CI, or a plain terminal): dotnet is
# deliberately not added to the shell profile permanently on this machine (see
# docs/architecture-decisions.md, point 7), and Make's exported PATH doesn't always reach
# direct-exec'd child processes on older GNU Make versions.
DOTNET="$(command -v dotnet 2>/dev/null || echo "$HOME/.dotnet/dotnet")"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT
PUBLISH_DIR="$WORK_DIR/publish"

echo "Publishing $EXECUTABLE_NAME for $RID ($CONFIGURATION, self-contained)..."
"$DOTNET" publish "$PROJECT" \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained true \
    -o "$PUBLISH_DIR"

mkdir -p "$OUT_DIR"
ZIP_PATH="$OUT_DIR/${APP_NAME}-${RID}.zip"
rm -f "$ZIP_PATH"

case "$RID" in
    osx-*)
        # Minimal, unsigned .app bundle so macOS users can (right-click ->) open it directly
        # instead of running a bare executable from a folder. Not code-signed/notarized -
        # Gatekeeper will warn on first launch, documented in README.md.
        BUNDLE_DIR="$WORK_DIR/${APP_NAME}.app"
        mkdir -p "$BUNDLE_DIR/Contents/MacOS" "$BUNDLE_DIR/Contents/Resources"
        cp -R "$PUBLISH_DIR"/. "$BUNDLE_DIR/Contents/MacOS/"
        cp packaging/macos/Info.plist "$BUNDLE_DIR/Contents/Info.plist"
        chmod +x "$BUNDLE_DIR/Contents/MacOS/$EXECUTABLE_NAME"
        ( cd "$WORK_DIR" && zip -r -y -q "$REPO_ROOT/$ZIP_PATH" "${APP_NAME}.app" )
        ;;
    linux-*|win-*)
        STAGE_DIR="$WORK_DIR/${APP_NAME}-${RID}"
        mkdir -p "$STAGE_DIR"
        cp -R "$PUBLISH_DIR"/. "$STAGE_DIR/"
        if [[ "$RID" == linux-* ]]; then
            chmod +x "$STAGE_DIR/$EXECUTABLE_NAME"
        fi
        ( cd "$WORK_DIR" && zip -r -q "$REPO_ROOT/$ZIP_PATH" "${APP_NAME}-${RID}" )
        ;;
    *)
        echo "Unknown RID '$RID' - expected osx-x64, osx-arm64, linux-x64, or win-x64." >&2
        exit 1
        ;;
esac

echo "Created $ZIP_PATH"
