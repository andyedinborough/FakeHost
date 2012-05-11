using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace MvcIntegrationTestFramework.Browsing {
  internal class SimulatedWorkerRequest : SimpleWorkerRequest {
    private HttpCookieCollection _Cookies;
    private readonly string _HttpVerbName;
    private readonly NameValueCollection _FormValues;
    private readonly NameValueCollection _Headers;
    private Uri _Uri;

    public override string GetServerName() {
      return _Uri.Host;
    }

    public SimulatedWorkerRequest(Uri uri, TextWriter output, HttpCookieCollection cookies, string httpVerbName, NameValueCollection formValues, NameValueCollection headers)
      : base(uri.AbsolutePath, uri.Query, output) {
      _Uri = uri;
      _Cookies = cookies;
      _HttpVerbName = httpVerbName;
      _FormValues = formValues;
      _Headers = headers;
    }

    public override string GetHttpVerbName() {
      return _HttpVerbName;
    }

    public override string GetKnownRequestHeader(int index) {
      // Override "Content-Type" header for POST requests, otherwise ASP.NET won't read the Form collection
      if (index == 12)
        if (string.Equals(_HttpVerbName, "post", StringComparison.OrdinalIgnoreCase))
          return "application/x-www-form-urlencoded";

      switch (index) {
        case 0x19:
          return MakeCookieHeader();
        default:
          if (_Headers == null)
            return null;
          return _Headers[GetKnownRequestHeaderName(index)];
      }
    }

    public override string GetUnknownRequestHeader(string name) {
      if (_Headers == null)
        return null;
      return _Headers[name];
    }

    public override string[][] GetUnknownRequestHeaders() {
      if (_Headers == null)
        return null;
      var unknownHeaders = from key in _Headers.Keys.Cast<string>()
                           let knownRequestHeaderIndex = GetKnownRequestHeaderIndex(key)
                           where knownRequestHeaderIndex < 0
                           select new[] { key, _Headers[key] };
      return unknownHeaders.ToArray();
    }

    public override byte[] GetPreloadedEntityBody() {
      if (_FormValues == null)
        return base.GetPreloadedEntityBody();

      var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
      foreach (string key in _FormValues)
        query[key] = _FormValues[key];
      return Encoding.UTF8.GetBytes(query.ToString());
    }

    private string MakeCookieHeader() {
      if ((_Cookies == null) || (_Cookies.Count == 0))
        return null;
      var sb = new StringBuilder();
      foreach (string cookieName in _Cookies)
        sb.AppendFormat("{0}={1};", cookieName, _Cookies[cookieName].Value);
      return sb.ToString();
    }
  }
}