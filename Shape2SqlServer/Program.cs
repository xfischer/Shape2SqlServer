#nullable enable
using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Shape2SqlServer.Core;

namespace Shape2SqlServer;

static class Program
{
	/// <summary>
	/// Point d'entrée principal de l'application.
	/// </summary>
	[STAThread]
	static void Main()
	{
		// Initialize logger factory for the WinForms application
		InitializeLogger();

		// Modern Microsoft.SqlServer.Types package (v170+) handles native assembly loading automatically
		// SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new frmMain());
	}

	private static void InitializeLogger()
	{
		Shape2SqlServerLoggerFactory.LoggerFactory = LoggerFactory.Create(builder =>
		{
			builder
				.AddSimpleConsole(options =>
				{
					options.SingleLine = true;
					options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
				})
				.AddDebug()
				.SetMinimumLevel(LogLevel.Information);
		});
	}
}
