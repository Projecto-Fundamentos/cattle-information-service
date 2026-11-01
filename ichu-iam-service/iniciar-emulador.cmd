@echo off
setlocal
cd /d "%~dp0"
where npx >nul 2>&1
if errorlevel 1 (
  echo Se necesita Node.js 22 o superior con npm para ejecutar el emulador.
  echo Instala Node.js, abre otra terminal y vuelve a ejecutar este archivo.
  pause
  exit /b 1
)
if exist ".firebase-emulator\firebase-export-metadata.json" (
  call npx --yes firebase-tools@15.33.0 emulators:start --only auth --project demo-ichu --import=.firebase-emulator --export-on-exit=.firebase-emulator
) else (
  call npx --yes firebase-tools@15.33.0 emulators:start --only auth --project demo-ichu --export-on-exit=.firebase-emulator
)
pause
