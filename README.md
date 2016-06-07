# MasterFudge
MasterFudge is an incomplete Sega Master System and Game Gear emulator written in C#. It is not very accurate, is missing features (see below for details) and is generally somewhat fudged together, hence its name.

## Status
* __Z80__: All documented opcodes done (main, CB-, DD-, ED- and FD-prefix; only a few "undocumented" ones), possibly still with issues (flags, etc.)
* __VDP__: Mostly done, but with inaccurate timing, some bugs here and there, and missing display modes 0 and 3
* __Memory__: Working, I guess; nothing fancy here
* __Cartridges__: Basic Sega mapper support (ROM and RAM), can also save and load cartridge RAM 
* __Input__: P1 joypad/Game Gear buttons, SMS Pause and Reset are implemented and work
* __Sound__: Some really crappy PSG emulation and missing the noise channel
* __Misc__: Region selection; support for SMS and GG bootstrap/BIOS ROMs; overall timing isn't very good

Otherwise, if it's not listed above, it's probably not there yet at all.

## Screenshots
* __Sonic the Hedgehog__:<br>
 ![Screenshot 1](http://i.imgur.com/l3dbCzW.png) ![Screenshot 2](http://i.imgur.com/R7wxWex.png)<br><br>
* __Ys - The Vanished Omens__:<br>
 ![Screenshot 3](http://i.imgur.com/3Z0QbIr.png) ![Screenshot 4](http://i.imgur.com/sKfIdqx.png)<br><br>
* __Castle of Illusion__:<br>
 ![Screenshot 5](http://i.imgur.com/8OxXcHF.png) ![Screenshot 6](http://i.imgur.com/TXJgBPs.png)<br><br>
