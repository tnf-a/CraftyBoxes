The CraftFromContainers feature from OdinQOL, pulled out for your modular pleasure.


`This mod uses ServerSync internally. Settings can change live through the BepInEx Configuration manager (if you are in game) or by directly changing the file on the server. Can be installed on both the client and the server to enforce configuration.`


### Request of the community to make it modular, resulted in separation of features. This is the CraftFromContainers OdinQOL version. Modified for compatibility with WardIsLove and serversync.


> ## Installation Instructions
***You must have BepInEx installed correctly! I can not stress this enough.***

#### Windows (Steam)
1. Locate your game folder manually or start Steam client and :
    * Right click the Valheim game in your steam library
    * "Go to Manage" -> "Browse local files"
    * Steam should open your game folder
2. Extract the contents of the archive into the BepInEx\plugins folder.
3. Locate odinplus.qol.CraftyBoxes.cfg under BepInEx\config and configure the mod to your needs

#### Server

`If installed on both the client and the server syncing to clients should work properly.`
1. Locate your main folder manually and :
   a. Extract the contents of the archive into the BepInEx\plugins folder.
   b. Launch your game at least once to generate the config file needed if you haven't already done so.
   c. Locate odinplus.qol.CraftyBoxes.cfg under BepInEx\config on your machine and configure the mod to your needs
2. Reboot your server. All clients will now sync to the server's config file even if theirs differs. Config Manager mod changes will only change the client config, not what the server is enforcing.


`Feel free to reach out to me on discord if you need manual download assistance.`


# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/


For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>

***
> # Update Information (Latest listed first)

> ### v1.0.4
> * Reflect the additions to CFC from OdinQOL.
>   * CFC now has a configuration option for ItemDisallowTypes, to prevent certain items from being pulled into the player inventory.
> ### v1.0.3
> * WardIsLove compatibility update for v3.0.1
> #### v1.0.2
> * Reflect the fixes from OdinQOL for these Potentially game breaking fixes:
>    * Fix a bug where the requirement was more than the max stack size or the item can't be stacked would cause a rouge
>      item to be left in the chest. (Thanks to Lime18 for the bug report & Bjorn for the fix!)
>    * Fix a bug where pulling resources while having the stack size multiplied would cause an increase in the stack size
>      & sometimes the actual item count. (Thanks to Bjorn for the bug report & fix!)
>    * Fix for Pulling resources into the player inventory not grabbing all resources needed to craft if the item doesn't
>      stack. (Found when testing Bjorn's fix!)
> * Remove incompatibility with Mod Settings mod. Will add back if issues with that mod arise.
> #### v1.0.1
> - Reflect the changes made in OdinsQOL
>   - Fixed FillAllKey not working. Keyboardshortcut was always returning false, not sure why.
> #### v1.0.0
> - Initial Release