@echo off
rem Starts LocalFoundry.Api and opens the browser test console once it is listening.
rem Closing this window stops the API.
setlocal

rem Change this if port 5189 is already in use on your machine.
set "PORT=5189"

cd /d "%~dp0"
title LocalFoundry.Api - http://localhost:%PORT%/ (close this window to stop)

rem Open the browser in the background after a short delay so the server has time to bind
rem the port. 'ping' is used as the delay because 'timeout' fails without an interactive console.
start "" /min cmd /c "ping -n 4 127.0.0.1 >nul & start "" http://localhost:%PORT%/"

"%~dp0LocalFoundry.Api.exe" --urls "http://localhost:%PORT%"

if errorlevel 1 (
  echo.
  echo LocalFoundry.Api stopped with an error. If port %PORT% is already in use,
  echo change PORT at the top of "%~f0".
  pause
)
