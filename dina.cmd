@echo off
pushd
@setlocal
set ERROR_CODE=0

src\Dina.Console\bin\Debug\net8.0\Dina.Console.exe %*

:end
@endlocal
popd
exit /B %ERROR_CODE%