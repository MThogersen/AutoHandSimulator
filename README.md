# AutoHandSimulator
A keyboard/mouse input to simulate movement and rotation in the Unity AutoHand package.

![ Alt text](AutoHandSim.gif) 

(Do you like the looks of it? - it's a work in progress. Follow me on [twitter](https://twitter.com/MTrobotics?ref_src=twsrc%5Etfw) , I will be sharing more about this game in the coming months :-) )

# Setup
* Add the Scripts folder somewhere in your Unity Project. 
* Attach the "AutoHandPlayerControllerInputSimulator" to AutoHandPlayer.

The script should(!) detect whether the Mock HMD is used and only be effective, if that is the case. 
If that is NOT the case, it will simply not run any code. 
Also, it is using pre-processor directives, so it won't compile for build versions, i.e. you should be able to just leave it there, even for a build version.

# Prerequisites
Requires: 
* The Mock HMD from unity, get it here: https://docs.unity3d.com/Packages/com.unity.xr.mock-hmd@1.0/manual/index.html
* The "old" input system from unity, which is the default for Autohand (AFAIK).
* The AutoHand package
