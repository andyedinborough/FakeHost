using FakeHost.Interception;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.SessionState;

namespace FakeHost.Browsing {
	internal class BrowsingSession {
		public HttpSessionState Session { get; private set; }
		public HttpCookieCollection Cookies { get; private set; }

		public BrowsingSession() {
			Cookies = new HttpCookieCollection();
		}

		public RequestResult ProcessRequest(Uri uri, string httpVerb, string formValues, NameValueCollection headers) {
			if (uri == null) throw new ArgumentNullException("url");

			// Perform the request
			LastRequestData.Reset();
			var output = new StringWriter();
			httpVerb = (httpVerb ?? "GET").ToUpper();
			var workerRequest = new SimulatedWorkerRequest(uri, output, Cookies, httpVerb, formValues, headers);
			var ctx = HttpContext.Current = new HttpContext(workerRequest);
			HttpRuntime.ProcessRequest(workerRequest);
			var response = LastRequestData.Response ?? ctx.Response;

			// Capture the output
			AddAnyNewCookiesToCookieCollection(response);
			Session = ctx.Session;
			return new RequestResult {
				ResponseText = output.ToString(),
				ActionExecutedContext = LastRequestData.ActionExecutedContext,
				ResultExecutedContext = LastRequestData.ResultExecutedContext,
				Response = response,
			};
		}

		private void AddAnyNewCookiesToCookieCollection(HttpResponse response) {
			if (response == null) return;

			var lastResponseCookies = response.Cookies;
			if (lastResponseCookies == null)
				return;

			foreach (string cookieName in lastResponseCookies) {
				HttpCookie cookie = lastResponseCookies[cookieName];
				if (Cookies[cookieName] != null)
					Cookies.Remove(cookieName);
				if ((cookie.Expires == default(DateTime)) || (cookie.Expires > DateTime.Now))
					Cookies.Add(cookie);
			}
		}
	}
}