### About RpiUO

RpiUO is a Ultima Online server emulator written in c#, aiming to run on a Raspberry Pi 2 hardware. Currently, tests are being made using Raspbian, a Debian-based Linux. To use it on Linux, you will need to install/compile Mono [http://www.mono-project.com/].

This project is a fork from ServUO, which is a fork from RunUO. It aims to improve its speed for use on this hardware restricted environment, while maintaining most of it functionality. The first idea is to retain the ServUO implementations, but refactoring most of it for code optimization.

Our final goal is to make a fast and full featured to run on Raspberry Pi 2. We are studing another OS for the raspberry, since there are some limitations on the arm7hf version of Mono.

### Version
Publish 2

Stable on:
2015/12/26

### Installation

Getting started with RpiUO is quite easy. Just run 'bash Compile.PI.bash' and its done!. This script will compile both the server binary and Ultima SDK binary for you. After this you can run the server by executing the linux bash command: 'mono initServer.exe'.

### Last Big Changes History

2015/12/28 => Removed all Linq, refactored some classes for a speed up.
2015/12/25 => Removed some Linq usage for better cpu performance and smaller memory footprint. 
2015/12/23 => Removed some compiler warning causes.
2015/12/21 => Updated project dependencies, serialization checks and small fixes.
2015/12/20 => Added minor changes to compile on Raspberry Pi 2.
