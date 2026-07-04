.PHONY: run build test package package-all clean

# dotnet is deliberately not added to the shell profile permanently (see
# docs/architecture-decisions.md, point 7). Resolving DOTNET to an absolute path here (instead
# of relying on PATH at recipe-execution time) avoids a GNU Make 3.81 quirk - confirmed on this
# machine's stock /usr/bin/make - where a plain, no-metacharacter recipe command (e.g. "dotnet
# test") is exec'd directly by Make itself using a PATH snapshot that does NOT reflect an
# `export PATH := ...` set inside the Makefile, even though `$$PATH` shows the updated value
# inside a shell-interpreted recipe line. An absolute path sidesteps any PATH search entirely.
export DOTNET_ROOT := $(HOME)/.dotnet
DOTNET := $(shell command -v dotnet 2>/dev/null || echo $(HOME)/.dotnet/dotnet)
DIST_DIR := dist

run:
	$(DOTNET) run --project src/PhotoSorter.App

build:
	$(DOTNET) build

test:
	$(DOTNET) test

# Detects the current machine's Runtime Identifier so `make package` needs no arguments.
HOST_RID := $(shell u=$$(uname -s); m=$$(uname -m); \
	if [ "$$u" = "Darwin" ]; then if [ "$$m" = "arm64" ]; then echo osx-arm64; else echo osx-x64; fi; \
	elif [ "$$u" = "Linux" ]; then echo linux-x64; \
	else echo win-x64; fi)

## Builds a self-contained, directly runnable package for this machine (runs tests first).
package: test
	./scripts/package.sh $(HOST_RID) $(DIST_DIR)
	@echo "Package ready in $(DIST_DIR)/"

## Builds packages for every supported platform (Windows, macOS Intel/Apple Silicon, Linux)
## from this one machine - cross-RID self-contained publish works regardless of host OS.
package-all: test
	./scripts/package.sh win-x64 $(DIST_DIR)
	./scripts/package.sh osx-x64 $(DIST_DIR)
	./scripts/package.sh osx-arm64 $(DIST_DIR)
	./scripts/package.sh linux-x64 $(DIST_DIR)

clean:
	rm -rf $(DIST_DIR)
	$(DOTNET) clean
