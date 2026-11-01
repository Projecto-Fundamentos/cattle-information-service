@echo off
rem Called by launchers inside their setlocal scope; no global environment changes.
set "IAM_WORK=%~dp0..\..\work"
if not exist "%IAM_WORK%\jdk21" set "IAM_WORK=%USERPROFILE%\Documents\Codex\2026-10-09\https-github-com-orgs-projecto-fundamentos\work"
if defined JAVA_HOME if exist "%JAVA_HOME%\bin\java.exe" if exist "%JAVA_HOME%\bin\javac.exe" goto found
set "JAVA_HOME="
if exist "%IAM_WORK%\jdk21" (
  for /d %%J in ("%IAM_WORK%\jdk21\jdk-*") do if exist "%%~fJ\bin\javac.exe" set "JAVA_HOME=%%~fJ"
)
if defined JAVA_HOME goto found
for /f "delims=" %%J in ('where javac.exe 2^>nul') do if not defined JAVA_HOME for %%B in ("%%~dpJ..") do set "JAVA_HOME=%%~fB"
if defined JAVA_HOME if exist "%JAVA_HOME%\bin\java.exe" goto found
echo No se encontro un JDK. Configura JAVA_HOME con un JDK 21 o superior.
echo JAVA_HOME debe apuntar a la carpeta del JDK, no a su subcarpeta bin.
exit /b 1
:found
if "%~1"=="--check" echo JAVA_HOME=%JAVA_HOME%
exit /b 0
