@echo off
if not exist ..\..\AScore_Console\bin goto DirectoryNotFound

echo.
echo Calling ..\..\AScore_Console\bin\Distribute_Program.bat
pushd ..\..\AScore_Console\bin

@echo on
call Distribute_Program.bat NoPause

@echo off
popd

echo.
echo Files distributed
echo.

goto done

:DirectoryNotFound
echo.
echo Directory not found: AScore_Console\bin
echo.

:Done
pause
