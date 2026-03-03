# .NET 9 Upgrade Report

## Project target framework modifications

| Project name                     | Old Target Framework | New Target Framework | Commits  |
|:---------------------------------|:--------------------:|:--------------------:|----------|
| Marketio_Shared.csproj           | net8.0               | net9.0               | 3f460de6 |
| Marketio_Web.csproj              | net8.0               | net9.0               | b6806449 |
| Marketio_WPF.csproj              | net8.0-windows       | net9.0-windows       | d7d3a568 |

## NuGet Packages

| Package Name                                         | Old Version | New Version | Commit ID |
|:-----------------------------------------------------|:-----------:|:-----------:|-----------|
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 8.0.22      | 9.0.13      | 1eb01f23  |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore    | 8.0.22      | 9.0.13      | 1eb01f23  |
| Microsoft.AspNetCore.Identity.UI                     | 8.0.22      | 9.0.13      | 1eb01f23  |
| Microsoft.AspNetCore.Localization                    | 2.3.9       | (Removed)   | 1eb01f23  |
| Microsoft.EntityFrameworkCore.SqlServer              | 8.0.22      | 9.0.13      | 1eb01f23  |
| Microsoft.EntityFrameworkCore.Tools                  | 8.0.22      | 9.0.13      | 1eb01f23  |
| Microsoft.Extensions.Localization                    | 10.0.3      | 9.0.13      | 1eb01f23  |

## All commits

| Commit ID | Description                                                                                    |
|:----------|:-----------------------------------------------------------------------------------------------|
| 1b7ea367  | Commit upgrade plan                                                                            |
| 3f460de6  | Update Marketio_Shared.csproj to target .NET 9.0                                               |
| b6806449  | Update Marketio_Web.csproj to target .NET 9.0                                                  |
| 1eb01f23  | Update NuGet package versions in Marketio_Web.csproj                                           |
| d7d3a568  | Update Marketio_WPF.csproj to target .NET 9.0                                                  |

## Project feature upgrades

### Marketio_Shared

- Target framework upgraded from net8.0 to net9.0

### Marketio_Web

- Target framework upgraded from net8.0 to net9.0
- All Microsoft.AspNetCore and EntityFrameworkCore packages upgraded to version 9.0.13
- Microsoft.AspNetCore.Localization package removed (functionality now included in framework)
- Microsoft.Extensions.Localization downgraded from 10.0.3 to 9.0.13 for compatibility

### Marketio_WPF

- Target framework upgraded from net8.0-windows to net9.0-windows

## Next steps

- Test de applicatie grondig om te controleren of alle functionaliteit correct werkt
- Voer alle unit tests uit als die beschikbaar zijn
- Controleer of er compiler warnings zijn die aandacht nodig hebben
- Overweeg om de branch te mergen naar de main branch na succesvolle tests
