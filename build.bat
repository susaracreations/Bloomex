@echo off
setlocal
echo ========================================================
echo   Compiling Figma Violet BlueStacks 4 Edition (With App Icon)...
echo ========================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% (
    echo Error: csc.exe compiler not found at %CSC%
    exit /b 1
)

%CSC% /target:winexe ^
      /win32manifest:app.manifest ^
      /win32icon:app.ico ^
      /resource:app.ico,AppIcon ^
      /resource:app_icon.png,AppLogoPng ^
      /resource:fonts\Inter-SemiBold.ttf,InterSemiBold ^
      /resource:fonts\Inter-Bold.ttf,InterBold ^
      /resource:fonts\Inter-Medium.ttf,InterMedium ^
      /resource:fonts\Inter-Regular.ttf,InterRegular ^
      /resource:assets\icon-home.png,IconHome ^
      /resource:assets\icon-trash.png,IconTrash ^
      /resource:assets\icon-info.png,IconInfo ^
      /resource:assets\card-performance.png,PerformanceGauge ^
      /resource:assets\card-optimization-callout.png,CardCallout ^
      /resource:assets\card-optimization-help-icon.png,CardHelpIcon ^
      /out:Bloomex.exe ^
      /optimize+ ^
      /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll ^
      AutoUpdater.cs BlueStacksRegistry.cs BlueStacksProcess.cs GameModeEngine.cs UIComponents.cs FigmaVioletForm.cs Program.cs

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed! Check the error output above.
    exit /b %errorlevel%
)

copy /Y Bloomex.exe BlueStacks4_Violet_Edition.exe >nul

echo.
echo [SUCCESS] Built successfully: Bloomex.exe (and BlueStacks4_Violet_Edition.exe)
echo ========================================================
