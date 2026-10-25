@echo off
setlocal
powershell.exe -NoProfile -File "%~dp0Open-StudyProject.ps1" %*
if errorlevel 1 pause
