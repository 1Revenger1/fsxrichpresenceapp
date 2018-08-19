# FSXRichPresence App

An application that creates a Rich Presence for all versions of FSX, P3D and XPlane 11.

![Alt text](example.png?raw=true "Title")

## Requirements
You need to have FSUIPC/XPUIPC installed!

## Installation
Unzip the included zip and it will work!

If you want this to start with your flight simulator, you will need to go to your dll.xml, which is in "%appData%\Roaming\Microsoft\FSX" for FSX users, or "%appData%\Roaming\Lockheed Martin\prepar3d\". Once there, put in your own app entry right above </SimBase.Document> like so:
```
  <Launch.Addon>
	<Name>FSXRichPresenceApp</Name>
	<Disabled>True</Disabled>
	<Path>C:\Path\to\FSX Rich Presence\FSXRichPresenceApp.exe</Path>
  </Launch.Addon>
```

## Other projects used in this
[Discord-rpc-CSharp](https://github.com/Lachee/discord-rpc-csharp)

[FSUIPC Client DLL for .NET by Paul Henty](http://fsuipc.paulhenty.com/#home)
