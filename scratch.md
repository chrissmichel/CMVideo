Random bugs
After upgrading nuget package for materialdesign

The build error `MC1000` indicates that the XAML compiler (usually invoked by ) is trying to load a design-time assembly `Microsoft.Windows.Design.Extensibility` that isn't available or properly referenced in your project. `Microsoft.WinFx.targets`
This commonly occurs when:
1. You have design-time attributes or custom controls that reference WPF designer extensibility APIs
2. The NuGet package containing this assembly is missing or corrupted
3. There's a version mismatch between referenced assemblies

## Solution
Let me check your project file and packages to identify the exact issue:
Reading CMVideo.csproj
Reading packages.config
The issue is with the design-time assemblies referenced in lines 59-64 of your .csproj file. These are causing the XAML compiler to fail because Microsoft.Windows.Design.Extensibility (which these design assemblies depend on) isn't available.
Fix
The design-time assemblies (Microsoft.Xaml.Behaviors.Design.dll and Microsoft.Xaml.Behaviors.DesignTools.dll) are only needed for Visual Studio's designer and shouldn't be referenced during build. Remove them from your project:**Fixed!** I've removed the problematic design-time assembly references that were causing the MC1000 error. The `Microsoft.Xaml.Behaviors.dll` reference (which your app actually needs at runtime) is kept, but the `.Design.dll` and `.DesignTools.dll` references (which only broke the build) have been removed.
Try rebuilding your project now - it should compile successfully.