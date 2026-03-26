@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
pushd "%ROOT%" >nul || exit /b 1
uv run python "%ROOT%\scripts\release_cli.py" %*
set "EC=%ERRORLEVEL%"
popd >nul
exit /b %EC%
