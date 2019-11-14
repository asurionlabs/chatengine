if %1.==. echo Specify the configuration (Debug, Release)& goto :EOF

set Configuration=%1

echo Clean old package
rd /s /q .\site
del /q .\ChatWeb.zip

echo Building Site
dotnet.exe publish -o site -c %Configuration%

if ERRORLEVEL 1 echo Build Failed& goto :EOF

echo Packaging Site
pushd site
c:\ama\zip.exe -r ../ChatWeb.zip *
popd
