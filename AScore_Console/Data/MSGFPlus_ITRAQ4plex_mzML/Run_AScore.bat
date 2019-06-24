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

%ExePath% -T:msgfplus -F:Dataset_W_S2_Fr_04_2May17_msgfplus_syn.txt -D:Dataset_W_S2_Fr_04_2May17.mzML -P:AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml -U:Dataset_W_S2_Fr_04_2May17_msgfplus_syn_plus_ascore.txt -L:LogFile.txt -Fasta:..\H_sapiens_M_musculus_RefSeq_Excerpt.fasta

rem On Linux, use
rem mono ../../AScore_Console.exe -T:msgfplus -F:Dataset_W_S2_Fr_04_2May17_msgfplus_syn.txt -D:Dataset_W_S2_Fr_04_2May17.mzML -P:AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml -U:Dataset_W_S2_Fr_04_2May17_msgfplus_syn_plus_ascore.txt -L:LogFile.txt -Fasta:../H_sapiens_M_musculus_RefSeq_Excerpt.fasta

:Done

pause
