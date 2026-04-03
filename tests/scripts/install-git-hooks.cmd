@echo off
setlocal

set HOOK=.git\hooks\pre-commit

(
echo @echo off
echo powershell -ExecutionPolicy Bypass -File "tests\scripts\run-local-checks.ps1"
) > "%HOOK%"

echo Installed pre-commit hook at %HOOK%
endlocal

