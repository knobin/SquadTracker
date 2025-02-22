# SquadTracker

A [BlishHUD](https://blishhud.com/) module for tracking squad members and their roles. 

## Features

* Tracking players joining or leaving the squad.
* Automatically detect party members who enter/leave your current instance.
* List the known characters of each player.
* Keep track of the players who left your instance, and their roles.
* Assign up to two roles to each party member.
* Define your own custom roles, with custom icons (WIP).

## Installation

Note that this module requires [ArcDPS](https://www.deltaconnected.com/arcdps/) and [arcdps_bridge](https://github.com/knobin/arcdps_bridge) to function. Both of these can be downloaded manually, arcdps_bridge has to be downloaded manually but you can download ArcDPS via the [Guild Wars 2 Unofficial Addon Manager](https://github.com/gw2-addon-loader/GW2-Addon-Manager). 

This fork of SquadTracker has to be installed manually. The original SquadTracker can be found in the [BlishHUD Module Package Manager](https://blishhud.com/docs/user/installing-modules).

### Building
1. Clone the repository ```git clone https://github.com/knobin/SquadTracker.git```
2. Open the solution file with Visual Studio and build it.
3. Find SquadTracker.bhm in SquadTracker/bin/ and place it in your BlishHUD modules folder, usually located at `C:\Users\YourUserName\Documents\Guild Wars 2\addons\blishhud\modules`

## Usage
After installing and enabling the module from Blish HUD, find the tab that looks like a commander tag in the main Blish HUD Window. 

There are two panels in the menu - Squad Members and Squad Roles. 

The Squad Members panel shows current and former squad/party members, along with roles that you have assigned to them. In order for players to appear as Current Squad Members, they must be in the same map instance as you. If they leave your map, they will be moved to the Former Squad Members panel (for now - we're working on a solution for that). As players enter/exit your squad or instance, the roles you assign to them will be retained.

If a player changes characters, their previous characters are displayed in a tooltip when mousing over their box. 

The Squad Roles panel allows you to add your own custom roles. At the top, type in the name of the role you want, and click Add. These roles will be saved to a file on your hard drive, and will be loaded up the next time you play. Once you've added a new role, it should be selectable from the dropdowns on the Squad Members panel.

In the settings for SquadTracker, the SquadInterface can be enabled to show a view that looks like the squad interface in the game where squad members can be right clicked on to edit their roles.

### Adding custom icons (WIP)
If you are comfortable editing a JSON file, you may add custom icons to the roles you define. The file is (usually) saved at `C:\Users\YourUserName\Documents\Guild Wars 2\addons\blishhud\squadtracker\roles.json`. Roles you add through the SquadTracker UI will appear in this JSON file. For example, here is the contents of a JSON file with two custom roles added - Tank and Banners. 
``` 
[
    {
        "Name": "Quickness",
        "IconPath": "icons\\quickness.png"
    },
    {
        "Name": "Alacrity",
        "IconPath": "icons\\alacrity.png"
    },
    {
        "Name": "Heal",
        "IconPath": "icons\\regeneration.png"
    },
    {
        "Name": "Power DPS",
        "IconPath": "icons\\power.png"
    },
    {
        "Name": "Condi DPS",
        "IconPath": "icons\\Condition_Damage.png"
    },
    {
        "Name": "Tank",
        "IconPath": "C:\\Users\\YourUserName\\Documents\\Guild Wars 2\\addons\\blishhud\\squadtracker\\Toughness.png"
    },
    {
        "Name": "Banners",
        "IconPath": ""
    }
]
```
In this example, the `Tank` role has been manually edited to fill in the `IconPath` value with the full path to the icon to use. The `Banners` role has no icon. Note that you must use a double backslash `\\` in the full path for your icon image. The location of the image doesn't matter, so long as you give the full path here. 
## Known Issues

## Contributing