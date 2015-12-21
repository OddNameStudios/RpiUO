### About RpiUO

RpiUO is a community driven Ultima Online server emulator written in c#, aiming to run on a Raspberry Pi 2 hardware using Raspbian, a Debian-based Linux. To use it on Linux, you will need to install/compile Mono [http://www.mono-project.com/].

This project is a fork from ServUO, which is a fork from RunUO. It aims to improve its speed for use on this hardware restricted environment, while maintaining most of it functionality. The first idea is to retain the ServUO implementations, but refactoring most of it for code optimization.

### Version
Publish 1

### Installation

Getting started with RpiUO is quite easy. Just run Compile.PI and follow the prompts. This script will compile both the server binary and Ultima SDK binary for you. After this you can run the server by executing the linux bash command: 'mono RpiUO.exe'.

More in dept advice and guides can be found in our wiki.