﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Represents the header that must be present for every server request
	/// </summary>
	public sealed class ApiHeaders
	{
		/// <summary>
		/// TODO: Remove this when https://github.com/dotnet/corefx/pull/26701 makes it into the sdk
		/// </summary>
		public const string ApplicationJson = "application/json";

		/// <summary>
		/// The <see cref="ApiVersion"/> header key
		/// </summary>
		const string ApiVersionHeader = "Api";

		/// <summary>
		/// The <see cref="Username"/> header key
		/// </summary>
		const string usernameHeader = "Username";

		/// <summary>
		/// The <see cref="InstanceId"/> header key
		/// </summary>
		const string instanceIdHeader = "Instance";

		/// <summary>
		/// The JWT authentication header scheme
		/// </summary>
		const string jwtAuthenticationScheme = "Bearer";

		/// <summary>
		/// The password authentication header scheme
		/// </summary>
		const string passwordAuthenticationScheme = "Password";

		/// <summary>
		/// The current <see cref="AssemblyName"/>
		/// </summary>
		internal static readonly AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

		/// <summary>
		/// The <see cref="Models.Instance.Id"/> being accessed
		/// </summary>
		public long? InstanceId { get; set; }

		/// <summary>
		/// The client's user agent
		/// </summary>
        public ProductHeaderValue UserAgent { get; }

		/// <summary>
		/// The client's API version
		/// </summary>
		public Version ApiVersion { get; }

		/// <summary>
		/// The client's JWT
		/// </summary>
		public string Token { get; }

		/// <summary>
		/// The client's username
		/// </summary>
		public string Username { get; }

		/// <summary>
		/// The client's password
		/// </summary>
		public string Password { get; }

		/// <summary>
		/// If the header uses password or JWT authentication
		/// </summary>
		public bool IsTokenAuthentication => Token != null;

		/// <summary>
		/// Construct <see cref="ApiHeaders"/> for JWT authentication
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/></param>
		/// <param name="token">The value of <see cref="Token"/></param>
        public ApiHeaders(ProductHeaderValue userAgent, string token) : this(userAgent, token, null, null)
        {
            if (userAgent == null)
                throw new ArgumentNullException(nameof(userAgent));
            if (token == null)
                throw new ArgumentNullException(nameof(token));
        }

		/// <summary>
		/// Construct <see cref="ApiHeaders"/> for password authentication
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/></param>
		/// <param name="username">The value of <see cref="Username"/></param>
		/// <param name="password">The value of <see cref="Password"/></param>
		public ApiHeaders(ProductHeaderValue userAgent, string username, string password) : this(userAgent, null, username, password)
        {
            if (userAgent == null)
                throw new ArgumentNullException(nameof(userAgent));
            if (username == null)
                throw new ArgumentNullException(nameof(username));
            if (password == null)
                throw new ArgumentNullException(nameof(password));
        }

		/// <summary>
		/// Construct and validates <see cref="ApiHeaders"/> from a set of <paramref name="requestHeaders"/>
		/// </summary>
		/// <param name="requestHeaders">The <see cref="RequestHeaders"/> containing the <see cref="ApiHeaders"/></param>
		public ApiHeaders(RequestHeaders requestHeaders)
        {
			var jsonAccept = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(ApplicationJson);
			if (!requestHeaders.Accept.Any(x => x.MediaType == jsonAccept.MediaType))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Client does not accept {0}!", ApplicationJson));

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgentValues) || !ProductInfoHeaderValue.TryParse(userAgentValues.FirstOrDefault(), out var clientUserAgent))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Missing {0} headers!", HeaderNames.UserAgent));

			//assure the client user agent has a name and version
			if (String.IsNullOrWhiteSpace(clientUserAgent.Product.Name) || !Version.TryParse(clientUserAgent.Product.Version, out var clientVersion))
				throw new InvalidOperationException("Malformed client user agent!");
			
			//make sure the api header matches ours
			if (!requestHeaders.Headers.TryGetValue(ApiVersionHeader, out var apiUserAgentHeaderValues) || !ProductInfoHeaderValue.TryParse(apiUserAgentHeaderValues.FirstOrDefault(), out var apiUserAgent) || apiUserAgent.Product.Name != assemblyName.Name)
				throw new InvalidOperationException("Missing API user agent!");

			if (!Version.TryParse(apiUserAgent.Product.Version, out var apiVersion))
				throw new InvalidOperationException("Malformed API version!");

			ApiVersion = apiVersion;
			UserAgent = clientUserAgent.Product;

			//check api version compatibility
			var ourVersion = assemblyName.Version;
			if (ourVersion.Major != ApiVersion.Major || ourVersion.Minor != ApiVersion.Minor || ourVersion.Build > ApiVersion.Build)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Given API version is incompatible with version {0}!", ourVersion));

			if (!requestHeaders.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authorization))
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Missing {0} header!", HeaderNames.Authorization));
			var auth = authorization.First();
			var splits = new List<string>(auth.Split(' '));
			var scheme = splits.First();
			if (String.IsNullOrWhiteSpace(scheme))
				throw new InvalidOperationException("Missing authentication scheme!");

			splits.RemoveAt(0);
			var parameter = String.Concat(splits);
			if (String.IsNullOrEmpty(parameter))
				throw new InvalidOperationException("Missing authentication parameter!");

			if(requestHeaders.Headers.TryGetValue(instanceIdHeader, out var instanceIdValues))
			{
				var instanceIdString = instanceIdValues.FirstOrDefault();
				if (instanceIdString != default && Int64.TryParse(instanceIdString, out var instanceId))
					InstanceId = instanceId;
			}

			switch (scheme)
			{
				case jwtAuthenticationScheme:
					Token = parameter;
					break;
				case passwordAuthenticationScheme:
					Password = parameter;
					var fail = !requestHeaders.Headers.TryGetValue(usernameHeader, out var values);
					if (!fail)
					{
						Username = values.FirstOrDefault();
						fail = String.IsNullOrWhiteSpace(Username);
					}
					if (fail)
						throw new InvalidOperationException("Missing Username header!");
					break;
				default:
					throw new InvalidOperationException("Invalid authentication scheme!");
			}
		}

		/// <summary>
		/// Construct <see cref="ApiHeaders"/>
		/// </summary>
		/// <param name="userAgent">The value of <see cref="UserAgent"/></param>
		/// <param name="token">The value of <see cref="Token"/></param>
		/// <param name="username">The value of <see cref="Username"/></param>
		/// <param name="password">The value of <see cref="Password"/></param>
		ApiHeaders(ProductHeaderValue userAgent, string token, string username, string password)
		{
			UserAgent = userAgent;
			Token = token;
			Username = username;
			Password = password;
			ApiVersion = assemblyName.Version;
		}

		/// <summary>
		/// Set <see cref="HttpRequestHeaders"/> using the <see cref="ApiHeaders"/>. This initially clears <paramref name="headers"/>
		/// </summary>
		/// <param name="headers">The <see cref="HttpRequestHeaders"/> to set</param>
		public void SetRequestHeaders(HttpRequestHeaders headers)
		{
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			headers.Clear();
			headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJson));
			if (IsTokenAuthentication)
				headers.Authorization = new AuthenticationHeaderValue(jwtAuthenticationScheme, Token);
			else
			{
				headers.Authorization = new AuthenticationHeaderValue(passwordAuthenticationScheme, Password);
				headers.Add(usernameHeader, Username);
			}
			headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
			headers.Add(ApiVersionHeader, ApiVersion.ToString());
			if(InstanceId.HasValue)
				headers.Add(instanceIdHeader, InstanceId.ToString());
		}
    }
}
