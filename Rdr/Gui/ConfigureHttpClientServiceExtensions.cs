using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace Rdr.Gui
{
	public static class ConfigureHttpClientServiceExtensions
	{
		public static void AddDefaultHttpClient(this IServiceCollection services, Action<HttpClient> configureHttpClient)
		{
			services
				.AddHttpClient(string.Empty)
				.ConfigureHttpClient(configureHttpClient)
				.ConfigurePrimaryHttpMessageHandler(CreateDefaultHttpMessageHandler);
		}

		public static void AddNoCrlCheckHttpClient(this IServiceCollection services, Action<HttpClient> configureHttpClient)
		{
			services
				.AddHttpClient(RdrLib.Constants.NoCrlCheckHttpClientName)
				.ConfigureHttpClient(configureHttpClient)
				.ConfigurePrimaryHttpMessageHandler(CreateNoCrlCheckHttpMessageHandler);
		}

		private static SocketsHttpHandler CreateDefaultHttpMessageHandler()
		{
			return new SocketsHttpHandler
			{
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.All,
				ConnectTimeout = TimeSpan.FromSeconds(30d),
				MaxAutomaticRedirections = 5,
				SslOptions = new SslClientAuthenticationOptions
				{
					AllowRenegotiation = false,
					ApplicationProtocols = new List<SslApplicationProtocol>
					{
						SslApplicationProtocol.Http11,
						SslApplicationProtocol.Http2
					},
					CertificateRevocationCheckMode = X509RevocationMode.Online,
#pragma warning disable CA5398 // these two choices are never wrong
					EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
#pragma warning restore CA5398
					EncryptionPolicy = EncryptionPolicy.RequireEncryption
				}
			};
		}

		private static SocketsHttpHandler CreateNoCrlCheckHttpMessageHandler()
		{
			SocketsHttpHandler handler = CreateDefaultHttpMessageHandler();

			handler.SslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;

			return handler;
		}
	}
}
