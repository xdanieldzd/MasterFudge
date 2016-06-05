# MasterFudge
MasterFudge is an incomplete Sega Master System and Game Gear emulator written in C#. It is not very accurate, is missing features (see below for details) and is generally somewhat fudged together, hence its name.

## Status
* __Z80__: All documented opcodes done (main, CB-, DD-, ED- and FD-prefix; only a few "undocumented" ones), possibly still with issues (flags, etc.)
* __VDP__: Mostly done, but with inaccurate timing and missing the "legacy" display modes
* __Memory__: Working, I guess; nothing fancy here
* __Cartridges__: Basic Sega mapper support (ROM and RAM), does not save cartridge RAM yet
* __Input__: P1 joypad/Game Gear buttons, SMS Pause and Reset are implemented and work
* __Sound__: Some really crappy PSG emulation and missing the noise channel
* __Misc__: Region selection; overall timing isn't very good

Otherwise, if it's not listed above, it's probably not there yet at all.

## Known Issues
Known issues as of [this commit](https://github.com/xdanieldzd/MasterFudge/tree/c36eb1da2d8de7b6d20c43bbfe070f5466e25137); there's most likely more:

* __Master System__
 * __F-16 Fighting Falcon__: Uses non-SMS VDP mode; not implemented
 * __[VDP Test ROM](http://www.smspower.org/Homebrew/SMSVDPTest-SMS)__: Fails most HCounter tests, sprite off-screen Y collision test
* __Game Gear__
 * __Fantasy Zone Gear__: Bad horizontal scrolling on title screen

## Screenshots
* __Sonic the Hedgehog__:<br>
 ![Screenshot 1](http://i.imgur.com/l3dbCzW.png) ![Screenshot 2](http://i.imgur.com/R7wxWex.png)<br><br>
* __Ys - The Vanished Omens__:<br>
 ![Screenshot 3](http://i.imgur.com/3Z0QbIr.png) ![Screenshot 4](http://i.imgur.com/sKfIdqx.png)<br><br>
* __Castle of Illusion__:<br>
 ![Screenshot 5](http://i.imgur.com/8OxXcHF.png) ![Screenshot 6](http://i.imgur.com/TXJgBPs.png)<br><br>
