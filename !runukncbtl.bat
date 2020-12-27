@echo off
set rt11dsk=C:\bin\rt11dsk

if exist x-ukncbtl\DESOLA.BIN del x-ukncbtl\DESOLA.BIN
rem D:\Work\MyProjects\ukncbtl-utils\Release\Sav2Cart.exe DESOLA.SAV DESOLA.BIN
rem move DESOLA.BIN x-ukncbtl\DESOLA.BIN

del x-ukncbtl\desolate.dsk
@if exist "x-ukncbtl\desolate.dsk" (
  echo.
  echo ####### FAILED to delete old disk image file #######
  exit /b
)
copy x-ukncbtl\sys1002ex.dsk desolate.dsk
%rt11dsk% a desolate.dsk DESOLA.SAV
move desolate.dsk x-ukncbtl\desolate.dsk

@if not exist "x-ukncbtl\desolate.dsk" (
  echo ####### ERROR disk image file not found #######
  exit /b
)

start x-ukncbtl\UKNCBTL.exe /boot
