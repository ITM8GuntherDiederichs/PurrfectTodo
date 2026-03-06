@echo off
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5059
cd /d C:\data\itm8\Copilot\PurrfectTodo\PurrfectTodo
dotnet exec bin/run/PurrfectTodo.dll
