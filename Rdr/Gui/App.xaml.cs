using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FileLogger;
using RdrLib;

namespace Rdr.Gui
{
	public partial class App : Application
	{
		private readonly IHost host;
		private readonly ILogger<App> logger;

		public App()
		{
			InitializeComponent();

			host = BuildHost();

			logger = host.Services.GetRequiredService<ILogger<App>>();
		}

		private static IHost BuildHost()
		{
			return new HostBuilder()
				.ConfigureHostConfiguration(ConfigureHostConfiguration)
				.ConfigureLogging(ConfigureLogging)
				.ConfigureHostOptions(ConfigureHostOptions)
				.ConfigureAppConfiguration(ConfigureAppConfiguration)
				.ConfigureServices(ConfigureServices)
				.Build();
		}

		private static void ConfigureHostConfiguration(IConfigurationBuilder builder)
		{
			builder
				.AddCommandLine(Environment.GetCommandLineArgs())
				.AddEnvironmentVariables();
		}

		private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
		{
			builder.AddConfiguration(context.Configuration.GetSection("Logging"));

			builder.AddFileLogger();

			builder.AddEventLog();

			if (context.HostingEnvironment.IsDevelopment())
			{
				builder.AddDebug();
			}
		}

		private static void ConfigureHostOptions(HostBuilderContext context, HostOptions options)
		{
			options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
			options.ShutdownTimeout = TimeSpan.FromSeconds(5d);
		}

		private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
		{
			if (!context.HostingEnvironment.IsProduction())
			{
				string environmentMessage = $"environment is {context.HostingEnvironment.EnvironmentName}";

				Console.Out.WriteLine(environmentMessage);
				Console.Error.WriteLine(environmentMessage);
				Debug.WriteLineIf(Debugger.IsAttached, environmentMessage);
			}

			builder.SetBasePath(AppContext.BaseDirectory);

			const string permanent = "appsettings.json";
			string environment = $"appsettings.{context.HostingEnvironment.EnvironmentName}.json";

			builder
				.AddJsonFile(permanent, optional: false, reloadOnChange: true)
				.AddJsonFile(environment, optional: true, reloadOnChange: true);
		}

		private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<IFileLoggerSink, FileLoggerSink>();

			services.Configure<RdrOptions>(context.Configuration.GetRequiredSection("RdrOptions"));

			services.AddTransient<IRdrService, RdrService>();
			services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();
			services.AddTransient<MainWindow>();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			logger.LogDebug("startup started");

			host.Start();

			IHostApplicationLifetime appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

			IFileLoggerSink sink = host.Services.GetRequiredService<IFileLoggerSink>();

			sink.StartSink();

			using (host.Services.CreateScope())
			{
				MainWindow = host.Services.GetRequiredService<MainWindow>();	
			}

			appLifetime.ApplicationStopping.Register(() =>
			{
				MainWindow?.Close();
			}, useSynchronizationContext: true);

			MainWindow.Show();

			logger.LogInformation("started");
		}

		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (e.Exception is Exception ex)
			{
				logger.LogError(ex, "unhandled exception ({ExceptionType})", ex.GetType().FullName);
			}
			else
			{
				const string nullExceptionMessage = "dispatcher unhandled exception: inner exception was null";

				logger.LogCritical(nullExceptionMessage);

				Console.Error.WriteLine(nullExceptionMessage);
			}
		}
	}
}
