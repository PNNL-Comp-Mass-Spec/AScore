@echo off

set ExePath=AScore_Console.exe

if exist %ExePath% goto DoWork
if exist ..\%ExePath% set ExePath=..\%ExePath% && goto DoWork
if exist ..\..\Bin\Debug\%ExePath% set ExePath=..\..\Bin\Debug\%ExePath% && goto DoWork

echo Executable not found: %ExePath%
goto Done

:DoWork
echo.
echo Procesing with %ExePath%
echo.

%ExePath% -T:msgfplus -F:CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_msgfplus_fht.txt -D:CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_dta.txt -P:DynMetOx_DynPhos_stat_4plex_iodo_hcd.xml -L:LogFile.txt -U:CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_msgfplus_fht_WithAScore.txt

:Done

pause
