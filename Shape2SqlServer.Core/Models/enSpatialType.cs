#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shape2SqlServer.Core;

/// <summary>
/// Type of spatial column(s) to create
/// </summary>
public enum enSpatialType : int
{
	/// <summary>
	/// Geometry type only
	/// </summary>
	geometry = 0,
	/// <summary>
	/// Geography type only
	/// </summary>
	geography = 1,
	/// <summary>
	/// Both geometry and geography types
	/// </summary>
	both = 2
}
