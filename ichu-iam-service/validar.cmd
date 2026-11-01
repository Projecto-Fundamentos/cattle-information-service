@echo off
setlocal
cd /d "%~dp0"
call "%~dp0localizar-java.cmd"
if errorlevel 1 (
  pause
  exit /b 1
)
echo JAVA_HOME=%JAVA_HOME%
echo Compilando y ejecutando las pruebas. Esto puede tardar varios minutos.
if exist "%IAM_WORK%\apache-maven-3.9.9\bin\mvn.cmd" (
  call "%IAM_WORK%\apache-maven-3.9.9\bin\mvn.cmd" -B "-Dmaven.repo.local=%IAM_WORK%\m2" clean verify > validation.log 2>&1
) else (
  call "%~dp0mvnw.cmd" -B clean verify > validation.log 2>&1
)
set "IAM_RESULT=%ERRORLEVEL%"
type validation.log
if "%IAM_RESULT%"=="0" (
  echo.
  echo CORRECTO: compilacion y pruebas completadas.
) else (
  echo.
  echo La verificacion fallo. Comparte el archivo validation.log para revisarlo.
)
echo.
echo Resultado guardado en %CD%\validation.log
pause
exit /b %IAM_RESULT%
