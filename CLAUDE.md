# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ChromExplorer is a Windows WPF desktop application (.NET Framework 4.8) for viewing and processing chromatography / mass-spec data (MODAS chromatograms, NIST and Wiley library search, Mascot and mzML export). The solution is `ChromExplorer/ChromExplorer.sln` (~50 projects). The repo is under git (main branch `main`); `bin/`, `obj/`, and `packages/` are gitignored build output, but may exist on disk, so exclude them when searching.

## Build & test

Build with Visual Studio 2022 (MSBuild); there is no CLI build script. Per README:

- **Always build the x64 platform.** Many projects also have x86/AnyCPU configs, but x64 is the supported one.
- Requires the .NET 4.5 reference assemblies (see README) and the `Mindscape.LightSpeed 5.1.28` package from the local `ChromExplorer/nuget-legacy` folder (add it as a NuGet package source).
- Do **not** upgrade `CommonServiceLocator 1.0.0` or `Extended.Wpf.Toolkit 3.3.0`.

```bash
msbuild ChromExplorer/ChromExplorer.sln /p:Configuration=Debug /p:Platform=x64
```

Tests use MSTest (`Microsoft.VisualStudio.QualityTools.UnitTestFramework`) in the `*.Tests` projects (DataModel, ModasReader, DataProvider, ModasChromProvider, Viewer, NistLibrary, WileySearch). Run all or one:

```bash
vstest.console.exe path\to\ChromExplorer.DataModel.Tests.dll /Tests:TestMethodName
```

Installer: NSIS scripts in `Installer/` (`installer_x64.nsi`), driven by `Tools/makensisbuildrunner.exe` and `Tools/nsis_patcher.ps1`. The app is licensed via QLM (`qlm/`); the Licensing module is currently commented out in the bootstrapper.

Contributing: `main` is the default branch (PRs merge into it); work on a feature branch and open a pull request.

## Architecture

Prism + MEF modular app. The entry project `ChromExplorer/ChromExplorer` holds `App.xaml`, `Shell.xaml`, and `ChromExplorerBootstraper.cs` (a Prism `MefBootstrapper`).

- **Module registration is explicit.** `ChromExplorerBootstraper.ConfigureAggregateCatalog` adds one `AssemblyCatalog` per module/plugin assembly (the directory-scanning catalog is commented out). A new `ChromExplorer.Modules.*`, post-processing, or peak-picker project must be added there (and referenced by the entry project) or MEF will not discover it.
- **Feature modules** (`ChromExplorer.Modules.*`): Viewer (main chromatogram/spectrum UI, MVVM with `Views/` + `ViewModels/` + `Services/`), DataProvider, ModasChromProvider, MappedFileProvider, TestChromProvider, MS, Lc, Quan, Syringe, Settings, Status, Help, Auth, ModasGrpc, MascotExporter, MzMLExporter, plus library search (NistLibrary, WileyLibrary/WileySearch).
- **Pluggable processing via MEF exports**: peak pickers (`ChromExplorer.PeakPicking.*`, `PealPicking.EmptyPeakPicker`) and post-processing ops (`ChromExplorer.PostProcessing.*`: Gaussian, Median, Normalize, RemoveSpikes, Savitzky-Golay) are discovered and orchestrated by `Services.PickPeakerService` and `Services.PostProcessingService`.
- **Shared layers**: `ChromExplorer.Infrastructure` (region names, events, interfaces, behaviors, converters, settings base — what modules depend on instead of each other), `ChromExplorer.DataModel` (+ `.Interfaces`, `.Cmd`; chromatogram/spectrum model, file formats, resampling, LightSpeed ORM `DAL/`), `ChromExplorer.Controls`/`Plotter`/OxyPlot for charting, `ChromExplorer.Mocks` for test doubles (event aggregator, region manager, logger).
- **MODAS integration**: `ChromExplorer.ModasReader` (native-format reader), `ChromExplorer.ModasClient`, and the gRPC stack `Modas.Grpc.Client` / `.Client.Shared` / `.Controls` / `.Models` consumed by `Modules.ModasGrpc`. Instrument/serial helpers are in `Serial.Utils` and `Modules.Syringe`.
- Modules communicate through Prism `IEventAggregator` events and regions (defined in Infrastructure) rather than direct references; logging goes through `Log4NetLogger`/`CallbackLogger`.

Other top-level material: `CHANGELOG` (per-version notes), `ChromExplorer/docs/mascot.pdf`, `MigrationBackup/` and `UpgradeLog.htm` (VS upgrade artifacts, not source).
