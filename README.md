# Bits & Bops Archipelago

## Install Guide
1. Download [BepinEx](https://github.com/BepInEx/BepInEx/releases) 5.4 for x64 architecture
2. Extract all files into your Bits & Bops directory
3. Launch the game
4. Close the game
5. Go to BepinEx/config and open BepinEx.cfg in a text editor
6. Change HideManagerGameObject to true
7. Download the [latest release of this mod](https://github.com/xMcacutt-Archipelago/BitsAndBopsAP-Client/releases)
8. Extract the BitsAndBopsAPClient folder into the bepinex/plugins folder in your game folder
9. Launch the game
10. Bop 
11. Miss a beat 
12. Cry

## Implementation Details

This implementation primarily randomises the order that levels unlock.
To finish the seed, you much complete levels as specified in your yaml.
If you require completions at other speeds, the records for each level will be added to the pool.
With badgesanity turned on, the multiplayer cartridges will also be added to the pool.

Completing levels to the standard set in your yaml will send checks. 
Completing the badge achievements will also send checks when badgesanity is enabled.

To play the records at different speeds, you must have the record for the level. 
Then go to the record player and select the record for the level you want to play.
Setting the speed of the record on the record player and then selecting the level will play the level at that speed.

Deathlink is implemented. Receiving a death immediately quits the current level. 
Missing a beat has a 5% chance to send a death.

The apworld can be found [here](https://github.com/xMcacutt-Archipelago/Archipelago-Bits-and-Bops/releases/latest)