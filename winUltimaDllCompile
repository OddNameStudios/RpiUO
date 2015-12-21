@TITLE: RpiUO

::##########

@ECHO:
@ECHO: Compile Ultima SDK
@ECHO:

@PAUSE

@DEL "%CURPATH%Ultima.dll"

@ECHO ON

%CSCPATH%csc.exe /target:library /out:"%CURPATH%Ultima.dll" /recurse:"%SDKPATH%*.cs" /d:ServUO /nowarn:0618 /debug /nologo /optimize /unsafe

@ECHO OFF

@ECHO:
@ECHO: Done!
@ECHO:

@PAUSE

@CLS
