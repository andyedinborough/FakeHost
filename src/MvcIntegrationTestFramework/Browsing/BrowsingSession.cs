using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using MvcIntegrationTestFramework.Interception;

namespace MvcIntegrationTestFramework.Browsing {
  internal class BrowsingSession {
    public HttpSessionState Session { get; private set; }
    public HttpCookieCollection Cookies { get; private set; }

    public BrowsingSession() {
      Cookies = new HttpCookieCollection();
    }

    public RequestResult ProcessRequest(Uri uri) {
      return ProcessRequest(uri, HttpVerbs.Get, null);
    }

    public RequestResult ProcessRequest(Uri uri, HttpVerbs httpVerb, NameValueCollection formValues) {
      return ProcessRequest(uri, httpVerb, formValues, null);
    }

    public RequestResult ProcessRequest(Uri uri, HttpVerbs httpVerb, NameValueCollection formValues, NameValueCollection headers) {
      if (uri == null) throw new ArgumentNullException("url");
       
      // Perform the request
      LastRequestData.Reset();
      var output = new StringWriter();
      string httpVerbName = httpVerb.ToString().ToLower();
      var workerRequest = new SimulatedWorkerRequest(uri, output, Cookies, httpVerbName, formValues, headers);
      HttpRuntime.ProcessRequest(workerRequest);

      // Capture the output
      AddAnyNewCookiesToCookieCollection();
      Session = LastRequestData.HttpSessionState;
      return new RequestResult {
        ResponseText = output.ToString(),
        ActionExecutedContext = LastRequestData.ActionExecutedContext,
        ResultExecutedContext = LastRequestData.ResultExecutedContext,
        Response = LastRequestData.Response,
      };
    }

    private void AddAnyNewCookiesToCookieCollection() {
      if (LastRequestData.Response == null)
        return;

      HttpCookieCollection lastResponseCookies = LastRequestData.Response.Cookies;
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