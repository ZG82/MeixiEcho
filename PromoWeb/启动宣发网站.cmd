@echo off
cd /d "%~dp0"
start "" /b cmd /c "timeout /t 3 /nobreak >nul & start http://localhost:3000"
npm.cmd run dev
