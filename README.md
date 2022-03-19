# AutoHandSimulator
A keyboard/mouse input to simulate movement and hand controls for the [AutoHand](https://assetstore.unity.com/packages/tools/physics/auto-hand-vr-physics-interaction-165323) package for Unity.

![AutohandSim](AutoHandSim.gif) 

(Do you like the looks of it? - it's a work in progress. Follow me on [twitter](https://twitter.com/MTrobotics?ref_src=twsrc%5Etfw) , I will be sharing more about this game in the coming months :-) )

## Update! - Now includes basic hand control

![LegacyAutoHandSim](AutoHandSim_w_hands.gif) 

To control hands, simply hold down Q for left hand and E for right hand and move the mouse, to move the hands up/down/left/right and scroll to move forward/backwards.

# Setup
* Setup [AutoHand](https://assetstore.unity.com/packages/tools/physics/auto-hand-vr-physics-interaction-165323) for Unity.
* Copy `Assets` folder into your project
* Make sure `Mock HMD` is enabled: `Go to Edit -> Project Settings-> XR Plug-in Management -> Mock HMD Loader`
* Attach the `Assets/AutoHand/Scripts/DebugTools/AutoHandPlayerControllerInputSimulator.cs` to the **AutoHandPlayer**.

![DefaultConfig](ScriptLocation.png)

* If using unity version **2021.2.12f1**, make sure to enable the old input handler: `Go to Edit` -> `Project Settings` -> `Player` -> Under `Other Settings` under `Configuration` is the option `Active Input Handling`. Select `Both`.

The script should(!) detect whether the Mock HMD is used and only be effective, if that is the case. 
If that is **NOT** the case, the code will simply not run. 
Also, it is using pre-processor directives, so it won't compile for build versions, i.e. you should be able to just leave it there, even for a build version.

## Prerequisites
Requires: 
* The Mock HMD from unity, get it here: https://docs.unity3d.com/Packages/com.unity.xr.mock-hmd@1.0/manual/index.html
* The "old" input system from unity, which is the default for Autohand (AFAIK).
* The [AutoHand](https://assetstore.unity.com/packages/tools/physics/auto-hand-vr-physics-interaction-165323) package

## Known issues
- In the Demo scene locomotion using the script occasionally does not work. I have not been able to reliably repreoduce the problem - a restart of Unity usually fixes it.
- The raytracing of the hands when moving the hands is a little bit off, just because the screen is split in two in VR. You have to get used to it.

## Controls
Controls can be rebound to different key-bindings

- W,A,S,D -> Body movement
- HOLD Q-> Left hand movement
- Q + Scroll up -> Move Left hand away
- Q + Scroll down -> Move Left hand closer
- HOLD E -> Right hand movement
- E + Scroll up -> Move Right hand away
- E + Scroll down -> Move Right hand closer
- HOLD E + Q -> Both hands movement
- E + Q + Scroll up -> Move both hands away
- E + Q + Scroll down -> Move both hands closer
- R -> Reset hands to default position
- HOLD Q + LMB -> grab with left
- HOLD E + LMB -> grab with right
- HOLD Q + HOLD E + LMB -> grab with both hands

- ESC -> Once play testing, click inside the game view to lock your cursor to the game.
         Escape can be used to detach the cursor from the game window

The camera behaves just like a first person shooter. When moving the mouse the camera freezes and stays in position.

## Settings

![DefaultConfig](DefaultConfiguration.png)

| Category     | Setting                  | Description                                                               |
|--------------|--------------------------|---------------------------------------------------------------------------|
| Requirements | `Auto Hand Player`       | A ref to the script `Auto Hand Player`                                    |
| Requirements | `Dev Hand Left`          | The left `Hand` script                                                    |
| Requirements | `Left Dev Hand Offset`   | The `Transform` of the left hand offset                                   |
| Requirements | `Dev Hand Right`         | The right `Hand` script                                                   |
| Requirements | `Right Dev Hand Offset`  | The `Transform` of the right hand offset                                  |
| Key-Bindings | `Mouse Sensitivity`      | The sensitivity of the first person camera                                |
| Key-Bindings | `Control Left Hand` Key  | When hold, allows you to move the left hand to the position of the mouse  |
| Key-Bindings | `Control Right Hand` Key | When hold, allows you to move the right hand to the position of the mouse |
| Key-Bindings | `Reset Hand` Key         | When pressed reset the position of both hands relative to the headset     |
| Key-Bindings | `Crouch` Key             | Player is crouching when pressed                                          |
| Key-Bindings | `Primary Button` Key     | Button used to grab and release objects in the workspace                  |
| Camera       | `FOV`                    | Field of View                                                             |
| Misc         | `Head Height`            | The height of the player in Meters                                        |

## Contributions

New to old:

---
- Refactor -> [0x78f1935](https://github.com/0x78f1935)
    - Changed the way hands move (by raycast)
    - Added First Person view
    - Fixed grab functionality
    - Added reset keybinding
    - Added crouch keybinding
---
- Base Code / idea -> [MThogersen](https://github.com/MThogersen)
    - Initial commit
---