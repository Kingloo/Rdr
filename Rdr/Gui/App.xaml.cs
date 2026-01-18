using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FileLogger;
using RdrLib;
using RdrLib.Services.Loader;
using RdrLib.Services.Updater;
using RdrLib.Services.Downloader;
using static Rdr.EventIds.App;
using static Rdr.Gui.AppLoggerMessages;

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

			AddAndConfigureHttpClients(services, context.Configuration);

			services.AddTransient<FeedLoader>();
			services.AddTransient<FeedUpdater>();
			services.AddTransient<FeedUpdateHistory>();
			services.AddTransient<FileDownloader>();

			services.AddTransient<IRdrService, RdrService>();

			services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();
			services.AddTransient<MainWindow>();
		}

		private static void AddAndConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
		{
			RdrOptions rdrOptions = configuration
				.GetSection("RdrOptions")
				.Get<RdrOptions>()
			?? RdrOptions.Default;

			services.AddDefaultHttpClient(configureHttpClient);
			services.AddNoCrlCheckHttpClient(configureHttpClient);

			void configureHttpClient(HttpClient client)
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd(rdrOptions.CustomUserAgent);
				client.DefaultRequestVersion = HttpVersion.Version20;
				client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
				client.Timeout = TimeSpan.FromSeconds(60d);
			}
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			LogStartupStarted(logger);

			host.Start();

			IHostApplicationLifetime appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

			IFileLoggerSink sink = host.Services.GetRequiredService<IFileLoggerSink>();

			sink.StartSink();

			using (host.Services.CreateScope())
			{
				MainWindow = host.Services.GetRequiredService<MainWindow>();
			}

			appLifetime.ApplicationStopped.Register(() =>
			{
				MainWindow?.Close();
			}, useSynchronizationContext: true);

			MainWindow.Show();

			LogStartupFinished(logger);
		}

		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (e.Exception is Exception ex)
			{
				string exceptionType = ex.GetType()?.FullName ?? "unknown exception type";

				string innerExceptionType = ex.InnerException is Exception innerEx
					? innerEx.GetType()?.FullName ?? "unknown inner exception type"
					: "no inner exception";

				string stackTrace = ex.StackTrace?[..Math.Min(ex.StackTrace.Length, 500)] ?? string.Empty;

				LogDispatcherUnhandledException(logger, exceptionType, ex.Message, innerExceptionType, stackTrace);
			}
			else
			{
				LogDispatcherUnhandledExceptionEmpty(logger);

				Console.Error.WriteLine("dispatcher unhandled inner exception was null");
			}
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			if (e.ApplicationExitCode == 0)
			{
				LogExited(logger);
			}
			else
			{
				LogExitedNotZero(logger, e.ApplicationExitCode);
			}

			IFileLoggerSink sink = host.Services.GetRequiredService<IFileLoggerSink>();

			sink.StopSink();

			host.StopAsync().GetAwaiter().GetResult();

			host.Dispose();
		}
	}

	internal static partial class AppLoggerMessages
	{
		[LoggerMessage(StartupStartedId, LogLevel.Debug, "startup started")]
		internal static partial void LogStartupStarted(ILogger<App> logger);

		[LoggerMessage(StartupFinishedId, LogLevel.Information, "started")]
		internal static partial void LogStartupFinished(ILogger<App> logger);

		[LoggerMessage(DispatcherUnhandledExceptionId, LogLevel.Error, "dispatcher unhandled exception {ExceptionType} - '{Message}' - ({InnerExceptionType}) - {StackTrace}")]
		internal static partial void LogDispatcherUnhandledException(ILogger<App> logger, string ExceptionType, string message, string innerExceptionType, string stackTrace);

		[LoggerMessage(DispatcherUnhandledExceptionEmptyId, LogLevel.Critical, "dispatcher unhandled exception: inner exception was null")]
		internal static partial void LogDispatcherUnhandledExceptionEmpty(ILogger<App> logger);

		[LoggerMessage(ExitedId, LogLevel.Information, "exited")]
		internal static partial void LogExited(ILogger<App> logger);

		[LoggerMessage(ExitedNotZeroId, LogLevel.Error, "exited (exit code {ExitCode})")]
		internal static partial void LogExitedNotZero(ILogger<App> logger, int exitCode);
	}
}
