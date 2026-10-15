## Build Instructions (VS2022)

### Install .NET 4.5

 - Download [Microsoft.NETFramework.ReferenceAssemblies.net45](https://www.nuget.org/packages/microsoft.netframework.referenceassemblies.net45) from nuget.org
 - Open the package as zip (or rename the package file and change its extension from .nupkg to .zip then open it)
 - copy the files from build\.NETFramework\v4.5\ to C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\\.NETFramework\v4.5
 - Close Visual Studio and then reopen it

 ### Dependencies
  - Mindscape.LightSpeed.5.1.28 - add chromexplorer\nuget-legacy folder to the Package Sources in Visual Studio
  - All other dependencies are available on nuget.org
> [!IMPORTANT]
> Do not upgrade `CommonServiceLocator 1.0.0` and `Extended.Wpf.Toolkit 3.3.0`  

> [!IMPORTANT]
> Always build x64 platform  

## Contributing
 - Master branch is protected. 
 - To make changes create a new branch and subit a merge request when ready

## Additional Information
 - [[MS Simulator Instructions|Modas]]
 - [[Implementation Details|ImplementationDetailes]]
 
