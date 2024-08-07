### 3.1.3/3.1.4
- When using a cart, the camera will not collide with the cart.
### 3.1.2
- Update for Ashlands
- Update internal code to use the latest PieceManager code as well as some fixes to patches.
### 3.1.0
- Update to use the latest BepInEx, Valheim Version,  and ServerSync as references.
- Update to my latest PieceManager code.
- Update the carts to use the "Crafty Carts" category in the build menu by default now.
- Add in hash version checking for the mod. Client and Server versions now must match exactly.

### 3.0.8
- Update ServerSync internally
- Update PieceManager internally
### 3.0.7
- Fix loading issue due to latest PieceManager update.
    * Forgot to declare a category for the piece now. You can now change the category for each piece via the configuration manager/config file. (configuration manager will change it live)
### 3.0.6
- Update internal PieceManager code. Below are the specific changes you will notice in this mod.
    * Fix width in config manager, localization display, add building pieces in start up
### 3.0.5
- Add cart inventory size configs
- Compile against latest version of the game.
### 3.0.3/v3.0.4
- Never copy and paste kids! I'm sorry lol
### 3.0.2
- Fix Localization issue on hover text
### 3.0.1
- Fix the build/crafting bug (Thank you Zarboz for catching my derp!)
- Update Localizaton of the carts for all languages (except the cart description)
- Fix some visual bugs and hover issues

### 3.0.0
- Update all the code to now be completely different from the original. (Code can be found here [CraftyCartsRemake Github](https://github.com/AzumattDev/CraftyCartsRemake))
- Fixed issue with flicker of the cart's material by rebuilding the carts in Unity, not code.
- Logout and back in issue resolved by using my PieceManager's code.
- Made the carts harder to tip over. (Please note: By default, the heaver the cart gets...the harder it is to pull.)
- Added lights and storage to the carts. Making them a little more viable for roaming around.
- Config file renamed to `Azumatt.CraftyCarts.cfg` to reflect the new config options and not conflict with any old files
- Update to use the latest BepInEx, Valheim Version, and ServerSync.
- Update README.md on github and here. Added credits inside the code to Rolopogo. Thank you once again for an amazing mod idea. Glad that I got to continue the dream.
### 2.1.0
- Compatibility with RRR and custom mobs causing carts not to go into the object database.
- Add config option for cart weight changes.
### 2.0.0
- Remake/Port to Hearth & Home update, removed the need for a Lib.
### 1.1.0
- Carts are now able to see extensions in range
- Carts center of mass is now lower, so they should be more stable
### 1.0.1
- Readme fix
### 1.0.0 - (18/04/2021)
- Release