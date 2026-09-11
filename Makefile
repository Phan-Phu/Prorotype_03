UNITY   ?= $(shell ./tools/unity.sh)
PROJECT ?= $(CURDIR)
ART     := $(PROJECT)/Artifacts
BUILD   ?= local

BASE = "$(UNITY)" -batchmode -nographics -projectPath "$(PROJECT)"

.PHONY: compile test test-edit test-play build build-webgl build-win sim gate clean

compile:
	@mkdir -p $(ART)
	$(BASE) -quit -executeMethod Farm.Prototype.Editor.CICompile.Run -logFile -

test-edit:
	@mkdir -p $(ART)
	$(BASE) -runTests -testPlatform EditMode \
	  -testResults $(ART)/editmode.xml -logFile $(ART)/editmode.log

# B3/B5 (Sprint 1): first PlayMode test — boots the scene like a real build and asserts the tile
# grid actually rendered (Known Pitfalls #1/#2 in README), which test-edit alone can't see.
test-play:
	@mkdir -p $(ART)
	$(BASE) -runTests -testPlatform PlayMode \
	  -testResults $(ART)/playmode.xml -logFile $(ART)/playmode.log

test: test-edit

# DECISION_LOG 2026-08-15: WebGL module isn't installed on this machine; `build`/`gate` default to
# Win64 instead of failing every run. `build-webgl` still exists to try again once the module is
# installed or on a machine that has it.
build: build-win

build-webgl:
	@mkdir -p $(ART)
	$(BASE) -quit -buildTarget WebGL \
	  -executeMethod Farm.Prototype.Editor.CIBuild.BuildWebGL \
	  -buildNumber $(BUILD) -logFile $(ART)/build-webgl.log

build-win:
	@mkdir -p $(ART)
	$(BASE) -quit -buildTarget Win64 \
	  -executeMethod Farm.Prototype.Editor.CIBuild.BuildWin64 \
	  -buildNumber $(BUILD) -logFile $(ART)/build-win.log

sim:
	@mkdir -p $(ART)
	$(BASE) -quit -executeMethod Farm.Prototype.Editor.CISim.RunEconomy \
	  -simDays 30 -simSeed 42 -simOut $(ART)/economy.csv -logFile -

gate: compile test-edit test-play build-win
	@echo "GATE PASS -- build $(BUILD)"

clean:
	rm -rf $(ART)
