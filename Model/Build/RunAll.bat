
rem -------------------------------------------------------------
rem This batch file runs everything
rem -------------------------------------------------------------
	
start /WAIT %APSIM%\Model\ApsimRun /auto /autoclose %APSIM%

rem -------------------------------------------------------------
rem Go find all PostRunAll.bat files and execute them.
rem -------------------------------------------------------------
for /R %APSIM%\Tests %%d in (.) do (
   if EXIST %%d\PostRunAll.bat (
      pushd %%d
      call PostRunAll.bat
      popd
      )
   )

popd
call RunAllUnitTests.bat


