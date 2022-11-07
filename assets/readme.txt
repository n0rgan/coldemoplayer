compLexity Demo Player
Version: 1.1.15
http://code.google.com/p/coldemoplayer/
http://www.complexitygaming.com/

======================
1. FEATURES
======================
----------------------
1.1. Playback
----------------------

Supported games:

   Steam, Half-Life engine:
      -Counter-Strike
      -Team Fortress Classic
      -Day of Defeat
      -Deathmatch Classic
      -Half-Life: Opposing Force
      -Ricochet
      -Half-Life
      -Counter-Strike: Condition Zero
      -Counter-Strike: Condition Zero Deleted Scenes
      -Half-Life: Blue Shift

   Steam, Source engine:
      -Half-Life 2
      -Counter-Strike: Source
      -Half-Life: Source
      -Day of Defeat: Source
      -Half-Life 2: Lost Coast
      -Half-Life: Source Deathmatch
      -Half-Life 2: Episode One
      -Portal
      -Team Fortress 2
      -Garry's Mod
	  -Left 4 Dead

   Pre-Steam:
     -Half-Life and all mods (Half-Life engine version 1.0.0.4 and later).

----------------------
1.2. Playback options
----------------------

* Remove Fade to Black (POV demos)

Supported demos:
   -Counter-Strike
   -Counter-Strike: Source

* Remove Showscores (POV demos)

Supported demos:
   -Counter-Strike

* Start a Listen Server

Fixes the blank POV scoreboard and voice codec HLTV bugs).

Supported demos:
   -Any Steam, Half-Life engine game listed in the Playback section.

* Close when finished

Closes compLexity Demo Player automatically after demo playback is finished and the game process has been closed.

* Use HLAE

Allows the user to launch a game process via HLAE, giving access to a wide variety of features useful for movie making purposes.

----------------------
1.3. Analysis
----------------------

* Game log (deaths, chat text, etc.)
* Player and team scores by round
* Player settings
* A ping/loss graph for each player

Supported demos:
   -Counter-Strike, versions 1.0 through to 1.6.
   -Counter-Strike: Source (game log only).

----------------------
1.4. Demo conversion
----------------------

Demos are converted to, and played using the newest version of a game.

Supported demos:
   -Counter-Strike 1.0 to 1.5 and 1.6, using network protocols 46 and 47 (played using 1.6, network protocol 48)

----------------------
1.5. Map pool
----------------------

Demo files store a checksum (unique identifier) of the map they were recorded on. The map pool contains a collection of maps along with their corresponding checksums. Upon playback, if a demo's map checksum doesn't match the built-in map checksum, the map pool is searched. If a matching map is found in the map pool, it is used for playback.

Supported demos:
   -Counter-Strike 1.6 (and 1.0 to 1.5 if demo conversion is enabled).


======================
2. TROUBLESHOOTING
======================

Problem: compLexity Demo Player crashes on startup (with no information as to why).
Solution: The .NET Framework 3.0 runtime is required. See the website for more information.

Problem: Upon playback, the game launches but nothing happens; the demo doesn't play.
Solution: Go to the Preferences window and set your Steam account folder to whichever Steam account you are currently logged into.

Problem: Old Counter-Strike demos show incorrect player models (i.e. a Terrorist with a CT GIGN model).
Solution: Set cl_minmodels to 1.

Problem: Player shadows appear as a black squares.
Solution: Set cl_shadows to 0.


======================
3. LICENCE
======================

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

See copying.txt or http://www.gnu.org/licenses/ for a copy of the GNU General Public License.

======================
4. 3RD PARTY LICENCES
======================

JsonExSerializer
----------------
Copyright (c) 2007, Ted Elliott
All rights reserved.

http://code.google.com/p/jsonexserializer/
http://www.opensource.org/licenses/bsd-license.php
 
SharpZipLib
-----------
http://www.icsharpcode.net/OpenSource/SharpZipLib/

The library is released under the GPL with the following exception:

Linking this library statically or dynamically with other modules is making a combined work based on this library. Thus, the terms and conditions of the GNU General Public License cover the whole combination.

As a special exception, the copyright holders of this library give you permission to link this library with independent modules to produce an executable, regardless of the license terms of these independent modules, and to copy and distribute the resulting executable under terms of your choice, provided that you also meet, for each linked independent module, the terms and conditions of the license of that module. An independent module is a module which is not derived from or based on this library. If you modify this library, you may extend this exception to your version of the library, but you are not obligated to do so. If you do not wish to do so, delete this exception statement from your version. 

ZedGraph
--------
http://zedgraph.org/wiki/index.php?title=LGPL_License