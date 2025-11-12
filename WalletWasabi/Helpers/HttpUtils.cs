using GingerCommon.Logging;
using GingerCommon.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Tor.Http;

namespace WalletWasabi.Helpers;

public static class HttpUtils
{
	public record RequestBehavior(TimeSpan TotalTimeOut, TimeSpan RetryTimeOut, TimeSpan WaitTime, int MaxTries, bool LogSingleSuccess)
	{
		public static readonly RequestBehavior BaseBehavior = new(TimeSpan.FromMinutes(30), TimeSpan.FromDays(1), TimeSpan.FromMilliseconds(250), 0, false);
		public static readonly RequestBehavior NonCriticalBehavior = new(TimeSpan.FromMinutes(10), TimeSpan.FromDays(1), TimeSpan.FromMilliseconds(250), 3, false);
	}

	public static async Task<HttpResponseMessage> HttpSendJsonAsync(IHttpClient httpClient, HttpMethod method, string relativeUri, string jsonString, RequestBehavior behavior, CancellationToken cancellationToken)
	{
		var start = DateTime.UtcNow;

		using CancellationTokenSource absoluteTimeoutCts = new(behavior.TotalTimeOut);
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, absoluteTimeoutCts.Token);
		CancellationToken combinedToken = linkedCts.Token;

		int attempt = 1;
		do
		{
			try
			{
				using StringContent content = new(jsonString, Encoding.UTF8, "application/json");
				using CancellationTokenSource requestTimeoutCts = new(behavior.RetryTimeOut);
				using CancellationTokenSource requestCts = CancellationTokenSource.CreateLinkedTokenSource(combinedToken, requestTimeoutCts.Token);

				// Any transport layer errors will throw an exception here.
				HttpResponseMessage response = await httpClient.SendAsync(method, relativeUri, content, requestCts.Token).ConfigureAwait(false);

				TimeSpan totalTime = DateTime.UtcNow - start;
				if (attempt > 1)
				{
					Logger.LogDebug($"Received a response for {relativeUri} in {totalTime.TotalSeconds:0.##s} after {attempt} failed attempts.");
				}
				else if (behavior.LogSingleSuccess)
				{
					Logger.LogDebug($"Received a response for {relativeUri} in {totalTime.TotalSeconds:0.##s}.");
				}
				return response;
			}
			catch (HttpRequestException e)
			{
				Logger.LogTrace($"Attempt {attempt} to perform '{relativeUri}' failed with {nameof(HttpRequestException)}: {e.Message}.");
			}
			catch (OperationCanceledException e)
			{
				Logger.LogTrace($"Attempt {attempt} to perform '{relativeUri}' failed with {nameof(OperationCanceledException)}: {e.Message}.");
			}
			catch (Exception e)
			{
				Logger.LogDebug($"Attempt {attempt} to perform '{relativeUri}' failed with exception {e}.");
				throw;
			}

			// Wait before the next try.
			await Task.Delay(behavior.WaitTime, combinedToken).ConfigureAwait(false);

			attempt++;
			if (behavior.MaxTries > 0 && attempt > behavior.MaxTries)
			{
				string msg = $"Max tries {behavior.MaxTries} reach for {relativeUri}";
				Logger.LogDebug(msg);
				throw new OperationCanceledException(msg);
			}
		}
		while (true);
	}

	public static async Task<TResponse> SendAndReceiveAsync<TRequest, TResponse>(IHttpClient httpClient, HttpMethod method, string relativeUri, TRequest request, RequestBehavior behavior, CancellationToken cancellationToken, JsonSerializerOptions? jsonOptions = null) where TRequest : class
	{
		var requestString = JsonUtils.Serialize(request, jsonOptions ?? JsonUtils.OptionCaseInsensitive);
		var response = await HttpSendJsonAsync(httpClient, method, relativeUri, requestString, behavior, cancellationToken).ConfigureAwait(false);

		var resultString = "";
		try
		{
			resultString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		}
		catch
		{
		}

		if (!response.IsSuccessStatusCode || !resultString.StartsWith('{'))
		{
			throw new HttpRequestException($"HttpRequest error {response.StatusCode}: {resultString}");
		}

		return JsonUtils.Deserialize<TResponse>(resultString);
	}
}
