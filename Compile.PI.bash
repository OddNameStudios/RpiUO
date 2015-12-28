mcs -out:Ultima.dll -recurse:Ultima/*.cs -t:library -d:MONO -optimize+ -nowarn:0219 -r:System,System.Drawing -unsafe

mono -O=all --aot Ultima.dll

mcs -out:initServer.exe -recurse:Server/*.cs -d:MONO -optimize+ -nowarn:0219,0414 -r:System,System.Data,System.Drawing,System.Xml,Ultima.dll -unsafe

mono -O=all --aot initServer.exe