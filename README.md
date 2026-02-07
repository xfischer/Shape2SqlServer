# Shape2SqlServer [![Build](https://github.com/xfischer/Shape2SqlServer/actions/workflows/build.yml/badge.svg)](https://github.com/xfischer/Shape2SqlServer/actions/workflows/build.yml)

Easy and fast shapefile import to MS SQL Server

 ![Viewer](/images/Screenshot.PNG?raw=true "Shape2SqlServer")

Current release: V10.0.1 - <https://github.com/xfischer/Shape2SqlServer/releases>

Inspired from the great tool <http://www.sharpgis.net/page/Shape2SQL> by Morten Nielsen @dotMorten.

## Features

- **Fast bulk import** with optimized data loading
- **Coordinate system reprojection** with ProjNet
- **Flexible spatial type support**: geometry, geography, or both
- **Spatial index creation** with configurable grid density
- **Modern .NET 10.0** implementation with latest C# features
- **Structured logging** with Serilog and Microsoft.Extensions.Logging
- **Schema support** for organized database structure
- **Error handling and validation** with comprehensive logging
- **WKT fallback** for robust geometry conversion

## Requirements

- .NET 10.0
- SQL Server with spatial support

## Dependencies

### Main NuGet Packages

- **NetTopologySuite** (2.5.0) - Geometry and spatial operations
- **NetTopologySuite.IO.ShapeFile** (2.1.0) - Shapefile reading
- **NetTopologySuite.IO.SqlServerBytes** (2.1.0) - SQL Server spatial binary format
- **ProjNet** (2.1.0) - Coordinate system transformations
- **Microsoft.SqlServer.Types** (170.1000.7) - SQL Server spatial types
- **Serilog.Sinks.File** (7.0.0) - File logging
