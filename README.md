# Shape2SqlServer
Easy and fast shape file import to MS SQL Server

 ![Viewer](/images/Screenshot.PNG?raw=true "Shape2SqlServer")
 
Current release : V1.0 : https://github.com/xfischer/Shape2SqlServer/releases

Inspired from the great tool http://www.sharpgis.net/page/Shape2SQL by Morten Nielsen @dotMorten.
This tool is great but Morten does not gives source code.

I have rewritten it. It features :
- Bulk import (faster)
- Reprojection
- Support for geometry and geography
- Spatial index creation


# NuGet packages dependencies
Thanks to
- GeoAPI
- ProjNet
- NetTopologySuite
- NetTopologySuite.IO.GeoTools
- NetTopologySuite.IO.MsSqlSpatial
