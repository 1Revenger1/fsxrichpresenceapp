# FSXRichPresence App

An application that creates a Rich Presence for all versions of FSX, P3D and XPlane 11.

![Alt text](example.png?raw=true "Title")

## Requirements
You need to have FSUIPC/XPUIPC installed!

## Installation
1) Download the latest FSX Rich Presence from [here](https://github.com/1Revenger1/fsxrichpresenceapp/releases)
2) Unzip the zip and it will work!

If you want this to start with your flight simulator, you will need to go to your fsuipc.ini, which for FSX is in "/Modules" within the root FSX folder. P3D should have their fsuipc.ini in a similar place.
```
[Programs]
Run1=C:\Users\Avery Black\Desktop\Random Desktop Folder\FSX Rich Presence\FSXRichPresenceApp.exe
```  

Editing the exe.xml should work as well for FSX/P3D

### Additional steps for X-Plane
If you want the altitude reading to work, you need to go to where XPlane is installed, and then go to `/resources/plugins/XPUIPC/XPUIPCOffsets.cfg` Once you open this up, you then need to add these three lines to the end and save the file.
```
# Altitude
Dataref Altitude sim/flightmodel/position/elevation float
Offset 0x34B0 FLOAT64 1 r $Altitude
```

## Other projects used in this
[Discord-rpc-CSharp](https://github.com/Lachee/discord-rpc-csharp)

[FSUIPC Client DLL for .NET by Paul Henty](http://fsuipc.paulhenty.com/#home)
