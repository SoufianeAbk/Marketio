# .NET 9 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 9 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 9 upgrade.
3. Upgrade Marketio_Shared\Marketio_Shared.csproj
4. Upgrade Marketio_Web\Marketio_Web.csproj
5. Upgrade Marketio_WPF\Marketio_WPF.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                           | Current Version | New Version | Description                                          |
|:-------------------------------------------------------|:---------------:|:-----------:|:-----------------------------------------------------|
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore   | 8.0.22          | 9.0.13      | Recommended for .NET 9                               |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore      | 8.0.22          | 9.0.13      | Recommended for .NET 9                               |
| Microsoft.AspNetCore.Identity.UI                       | 8.0.22          | 9.0.13      | Recommended for .NET 9                               |
| Microsoft.AspNetCore.Localization                      | 2.3.9           |             | Package functionality included with framework        |
| Microsoft.EntityFrameworkCore.SqlServer                | 8.0.22          | 9.0.13      | Recommended for .NET 9                               |
| Microsoft.EntityFrameworkCore.Tools                    | 8.0.22          | 9.0.13      | Recommended for .NET 9                               |
| Microsoft.Extensions.Localization                      | 10.0.3          | 9.0.13      | Recommended for .NET 9                               |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### Marketio_Shared modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net9.0`

#### Marketio_Web modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net9.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore should be updated from `8.0.22` to `9.0.13` (*recommended for .NET 9*)
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore should be updated from `8.0.22` to `9.0.13` (*recommended for .NET 9*)
  - Microsoft.AspNetCore.Identity.UI should be updated from `8.0.22` to `9.0.13` (*recommended for .NET 9*)
  - Microsoft.AspNetCore.Localization should be removed (*package functionality included with framework*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `8.0.22` to `9.0.13` (*recommended for .NET 9*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `8.0.22` to `9.0.13` (*recommended for .NET 9*)
  - Microsoft.Extensions.Localization should be updated from `10.0.3` to `9.0.13` (*recommended for .NET 9*)

#### Marketio_WPF modifications

Project properties changes:
  - Target framework should be changed from `net8.0-windows` to `net9.0-windows`
