#nullable enable
using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Shape2SqlServer;

static class Program
{
	private static ServiceProvider? _serviceProvider;

	public static ServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized");

	/// <summary>
	/// Point d'entrée principal de l'application.
	/// </summary>
	[STAThread]
	static void Main()
	{
		// Configure dependency injection
		ConfigureServices();

		// Modern Microsoft.SqlServer.Types package (v170+) handles native assembly loading automatically
		// SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		// Resolve the main form from DI container
		var mainForm = _serviceProvider!.GetRequiredService<frmMain>();
		Application.Run(mainForm);
	}

	private static void ConfigureServices()
	{
		var services = new ServiceCollection();

		// Configure logging
		var logFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "shape2sqlserver-.log");

		var serilogLogger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.File(
				path: logFilePath,
				rollingInterval: RollingInterval.Day,
				fileSizeLimitBytes: 10_485_760, // 10 MB
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: 7)
			.CreateLogger();

		services.AddLogging(builder =>
		{
			builder
				.AddSimpleConsole(options =>
				{
					options.SingleLine = true;
					options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
				})
				.AddDebug()
				.AddSerilog(serilogLogger)
				.SetMinimumLevel(LogLevel.Information);
		});

		// Register forms
		services.AddTransient<frmMain>();
		services.AddTransient<frmSRIDSelector>();
		services.AddTransient<frmConnectionDialog>();

		_serviceProvider = services.BuildServiceProvider();
	}
}
