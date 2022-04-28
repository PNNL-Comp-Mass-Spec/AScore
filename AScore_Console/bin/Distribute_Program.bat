@echo on

xcopy Debug\*.exe C:\DMS_Programs\AScore /D /Y
xcopy Debug\*.dll C:\DMS_Programs\AScore /D /Y

xcopy Debug\*.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common /D /Y

xcopy Debug\*.exe F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\AScore /D /Y
xcopy Debug\*.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\AScore /D /Y

xcopy Debug\*.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\AScore /D /Y
xcopy Debug\*.dll \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\AScore /D /Y

xcopy Debug\*.dll \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\ /D /Y

@echo off
if "%1"=="NoPause" Goto done

if not "%1"=="NoPause" pause

:Done
