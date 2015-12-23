@SET CURPATH=%~dp0
@SET CSCPATH=%windir%\Microsoft.NET\Framework\v4.0.30319\

@SET SDKPATH=%CURPATH%Ultima\
@SET SRVPATH=%CURPATH%Server\

@TITLE: RpiUO

::##########

@ECHO:
@ECHO: Compile Server for Windows
@ECHO:

@PAUSE

@DEL "%CURPATH%RpiUO.exe"

@ECHO ON

%CSCPATH%csc.exe /win32icon:"%SRVPATH%servuo.ico" /r:"%CURPATH%Ultima.dll" /target:exe /out:"%CURPATH%RpiUO.exe" /recurse:"%SRVPATH%*.cs" /d:ServUO /nowarn:0618 /debug /nologo /optimize /unsafe

@ECHO OFF

@ECHO:
@ECHO: Done!
@ECHO:

@PAUSE

@CLS

::##########

@ECHO:
@ECHO: Ready To Run!
@ECHO:

@PAUSE

@CLS

@ECHO OFF

%CURPATH%RpiUO.exe
