# AutoHandSimulator
A keyboard/mouse input to simulate movement and rotation in the Unity AutoHand package.

# Setup
* Add the files to your Unity Project 
* Attach the "AutoHandPlayerControllerInputSimulator" to AutoHandPlayer.
* 
The script should(!) detect whether the Mock HMD is used and only be effective, if that is the case. 
If that is NOT the case, it will simply not run any code. 
Also, it is using pre-processor directives, so it won't compile for build versions, i.e. you should be able to just leave it there, even for a build version.

# Prerequisites
Requires: 
* The Mock HMD from unity, get it here: 
* The "old" input system from unity, which is the default for Autohand (AFAIK).
* The AutoHand package
