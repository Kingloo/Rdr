using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FileLogger;
using RdrLib;
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

			AddAndConfigureHttpClient(services);

			services.AddTransient<IRdrService, RdrService>();
			services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();
			services.AddTransient<MainWindow>();
		}

		private static void AddAndConfigureHttpClient(IServiceCollection services)
		{
			services.AddHttpClient("RdrService")
				.ConfigureHttpClient(static (HttpClient client) =>
				{
					client.Timeout = TimeSpan.FromSeconds(30d);
				})
				.ConfigurePrimaryHttpMessageHandler(static () =>
				{
					return new SocketsHttpHandler
					{
						AllowAutoRedirect = true,
						AutomaticDecompression = System.Net.DecompressionMethods.All,
						ConnectTimeout = TimeSpan.FromSeconds(10d),
						MaxAutomaticRedirections = 5,
						SslOptions = new SslClientAuthenticationOptions
						{
							AllowRenegotiation = false,
							ApplicationProtocols = new List<SslApplicationProtocol>
							{
								SslApplicationProtocol.Http11,
								SslApplicationProtocol.Http2
							},
							CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.Online,
#pragma warning disable CA5398 // these two choices are never wrong
							EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
#pragma warning restore CA5398
							EncryptionPolicy = EncryptionPolicy.RequireEncryption
						}
					};
				})
				.AddStandardResilienceHandler();
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

			appLifetime.ApplicationStopping.Register(() =>
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
				LogDispatcherUnhandledException(logger, ex.GetType()?.FullName ?? "unknown type");
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

		[LoggerMessage(DispatcherUnhandledExceptionId, LogLevel.Error, "dispatcher unhandled exception {UnhandledExceptionFullName}")]
		internal static partial void LogDispatcherUnhandledException(ILogger<App> logger, string unhandledExceptionFullName);

		[LoggerMessage(DispatcherUnhandledExceptionEmptyId, LogLevel.Critical, "dispatcher unhandled exception: inner exception was null")]
		internal static partial void LogDispatcherUnhandledExceptionEmpty(ILogger<App> logger);

		[LoggerMessage(ExitedId, LogLevel.Information, "exited")]
		internal static partial void LogExited(ILogger<App> logger);

		[LoggerMessage(ExitedNotZeroId, LogLevel.Error, "exited (exit code {ExitCode})")]
		internal static partial void LogExitedNotZero(ILogger<App> logger, int exitCode);
	}
}
