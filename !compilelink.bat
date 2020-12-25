@echo off
set rt11exe=C:\bin\rt11\rt11.exe
set pclink11=C:\bin\pclink11.exe

rem Define ESCchar to use in ANSI escape sequences
rem https://stackoverflow.com/questions/2048509/how-to-echo-with-different-colors-in-the-windows-command-line
for /F "delims=#" %%E in ('"prompt #$E# & for %%E in (1) do rem"') do set "ESCchar=%%E"

for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "DATESTAMP=%YYYY%-%MM%-%DD%"
for /f %%i in ('git rev-list HEAD --count') do (set REVISION=%%i)
echo Rev.%REVISION% %DATESTAMP%

echo VERSTR:	.ASCIZ "Rev.%REVISION% %DATESTAMP%" > VERSIO.MAC

@if exist DESOLA.LST del DESOLA.LST
@if exist DESOLA.OBJ del DESOLA.OBJ

%rt11exe% MACRO/LIST:DK: DESOLA.MAC

for /f "delims=" %%a in ('findstr /B "Errors detected" DESOLA.LST') do set "errdet=%%a"
if "%errdet%"=="Errors detected:  0" (
  echo COMPILED SUCCESSFULLY
) ELSE (
  findstr /RC:"^[ABDEILMNOPQRTUZ] " DESOLA.LST
  echo ======= %errdet% =======
  exit /b
)

@if exist DESOLA.MAP del DESOLA.MAP
@if exist DESOLA.SAV del DESOLA.SAV
@if exist DESOLA-11.MAP del DESOLA-11.MAP
@if exist DESOLA-11.SAV del DESOLA-11.SAV

%rt11exe% LINK DESOLA /MAP:DESOLA.MAP
@if exist DESOLA.MAP rename DESOLA.MAP DESOLA-11.MAP
@if exist DESOLA.SAV rename DESOLA.SAV DESOLA-11.SAV

%pclink11% DESOLA.OBJ /MAP > pclink11.log

fc.exe /b DESOLA-11.SAV DESOLA.SAV > fc.log
for /f "delims=" %%a in ('findstr /B "FC: " fc.log') do set "fcdiff=%%a"
if "%fcdiff%"=="FC: no differences encountered" (
  echo SAV FILES ARE EQUAL
  del fc.log
  echo.
) ELSE (
  echo %ESCchar%[91m======= SAV FILES ARE DIFFERENT, see fc.log =======%ESCchar%[0m
  exit /b
)

for /f "delims=" %%a in ('findstr /B "Undefined globals" DESOLA.MAP') do set "undefg=%%a"
if "%undefg%"=="" (
  type DESOLA.MAP
  echo.
  echo %ESCchar%[92mLINKED SUCCESSFULLY%ESCchar%[0m
) ELSE (
  echo %ESCchar%[91m======= LINK FAILED =======%ESCchar%[0m
  exit /b
)
