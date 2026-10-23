@echo off
setlocal
set "EDGE=%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"
if not exist "%EDGE%" set "EDGE=%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"
if not exist "%EDGE%" (
  echo Edge nao foi localizado em Program Files.
  echo Abra o Edge manualmente com --remote-debugging-port=9333.
  pause
  exit /b 1
)
set "PROFILE=%LOCALAPPDATA%\GarimpoDaVezStudio\EdgeDebugProfile"
start "" "%EDGE%" --remote-debugging-address=127.0.0.1 --remote-debugging-port=9333 --user-data-dir="%PROFILE%" https://chatgpt.com
endlocal
