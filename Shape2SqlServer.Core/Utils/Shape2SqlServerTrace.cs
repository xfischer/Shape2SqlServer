#nullable enable

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Shape2SqlServer.Core;

/// <summary>
/// Legacy tracing class maintained for backward compatibility.
/// New code should use <see cref="Shape2SqlServerLoggerFactory"/> instead.
/// </summary>
[Obsolete("Use Shape2SqlServerLoggerFactory.Logger instead for modern logging with Microsoft.Extensions.Logging")]
public static class Shape2SqlServerTrace
{
	private static TraceSource? _source;

	/// <summary>
	/// Gets the shared trace source used for logging errors and diagnostic information within the Shape2SqlServer component.
	/// </summary>
	/// <remarks>
	/// The returned trace source is initialized with the name "Shape2SqlServer" and a default source level of <see cref="SourceLevels.Error"/>.
	/// This property always returns the same instance for the lifetime of the application.
	/// <para><strong>Obsolete:</strong> Consider using <see cref="Shape2SqlServerLoggerFactory.Logger"/> for modern logging with Microsoft.Extensions.Logging.</para>
	/// </remarks>
	public static TraceSource Source
	{
		get
		{
			if (_source == null)
			{
				_source = new TraceSource("Shape2SqlServer", SourceLevels.Error);
			}
			return _source;
		}
	}
}
