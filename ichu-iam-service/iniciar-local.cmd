@echo off
setlocal
cd /d "%~dp0"
call "%~dp0localizar-java.cmd"
if errorlevel 1 (
  pause
  exit /b 1
)
if not exist "target\ichu-iam-service-0.2.0.jar" (
  echo Primero ejecuta validar.cmd y comprueba BUILD SUCCESS.
  pause
  exit /b 1
)
set "FIREBASE_PROJECT_ID=demo-ichu"
set "FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099"
echo Debes tener iniciar-emulador.cmd abierto. Swagger: http://localhost:8080/swagger-ui/index.html
"%JAVA_HOME%\bin\java.exe" -jar "target\ichu-iam-service-0.2.0.jar" --spring.profiles.active=local
pause
