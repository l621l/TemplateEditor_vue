@echo off
start "TemplateEditor backend" cmd /k ""%~dp0runBack.bat""
start "TemplateEditor frontend" cmd /k ""%~dp0runFront.bat""
