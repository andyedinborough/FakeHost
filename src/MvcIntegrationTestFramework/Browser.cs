using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MvcIntegrationTestFramework.Hosting;

namespace MvcIntegrationTestFramework {
  public class Browser : IDisposable {
    private static object @lock = new object();
    private static AppHost _appHost;

    public Browser() {
      Cookies = new HttpCookieCollection();
    }

    private static bool IsInitialized() {
      return _appHost != null;
    }

    /// <summary>
    /// Initializes the ASP net runtime.
    /// </summary>
    /// <param name="pathToYourWebProject">
    /// The path to your web project. This is optional if you don't
    /// specify we try to guess that it is in the first directory like
    /// ../../../*/web.config
    /// </param>
    /// <remarks>
    /// Has been known to cause severe damage to your immortal soul.
    /// </remarks>
    public static void InitializeAspNetRuntime(string pathToYourWebProject = null, string virtualPath = null, string hostname = null) {
      if (_appHost == null)
        lock (@lock)
          if (_appHost == null) {
            if (pathToYourWebProject == null) {
              var guessDirectory = new DirectoryInfo(
                                      Path.GetFullPath(
                                          Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..")));

              var projectDirs = guessDirectory.GetDirectories();
              foreach (var pd in projectDirs) {
                if (pd.GetFiles("web.config").Length == 1) {
                  pathToYourWebProject = pd.FullName;
                  continue;
                }
              }
            }

            var ourDll0 = new Uri(typeof(MvcIntegrationTestFramework.Browser).Assembly.Location).LocalPath;
            var ourDll1 = Path.Combine(pathToYourWebProject, "bin", System.IO.Path.GetFileName(ourDll0));
            File.Copy(ourDll0, ourDll1, true);

            _appHost = new AppHost(pathToYourWebProject, virtualPath ?? "/");
          }
    }

    public HttpCookieCollection Cookies { get; internal set; }

    [NonSerialized]
    private System.Net.WebHeaderCollection _Headers = new System.Net.WebHeaderCollection();
    public System.Net.WebHeaderCollection Headers {
      get { return _Headers; }
    }

    public void AppendHeader(string name, string value) {
      Headers.Add(name, value);
    }

    /// <summary>
    /// Sends a post to your url.  
    /// </summary>
    /// <param name="url"></param>
    /// <param name="formData"></param>
    /// <example>
    /// <code>
    /// Post("registration/create", new
    /// {
    ///     Form = new
    ///     {
    ///         InvoiceNumber = "10000",
    ///         AmountDue = "10.00",
    ///         Email = "chriso@innovsys.com",
    ///         Password = "welcome",
    ///         ConfirmPassword = "welcome"
    ///     }
    /// });
    /// </code>
    /// </example>
    protected Response Send(string url, object formData = null, HttpVerbs method = HttpVerbs.Get) {
      var response = new Response();
      var formNameValueCollection = formData == null ? null : ConvertFromObject(formData);
      var headerCollection = System.Web.HttpUtility.ParseQueryString(string.Empty);
      foreach (string header in Headers) {
        headerCollection[header] = Headers[header];
      }

      var cookies = SerializableCookie.GetCookies(Cookies);

      lock (_appHost)
        _appHost.SimulateBrowsingSession(browser => {
          SerializableCookie.Update(browser.Cookies, cookies);

          var result = browser.ProcessRequest(url, method, formNameValueCollection, headerCollection);
          response.StatusCode = result.Response.StatusCode;
          response.ResponseText = result.ResponseText;
          response._SerializableCookies = SerializableCookie.GetCookies(browser.Cookies);

          var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
          var _customHeaders = GetPrivateInstanceField<System.Collections.ArrayList>(result.Response, "_customHeaders");
          foreach (var hdr in _customHeaders) {
            var name = GetPrivateInstanceProperty<string>(hdr, "Name");
            var value = GetPrivateInstanceProperty<string>(hdr, "Value");
            query[name] = value;
          }

          response.RawHeaders = query.ToString();
        });

      SerializableCookie.Update(Cookies, response._SerializableCookies);

      return response;
    }

    private static T GetPrivateInstanceField<T>(object obj, string name) {
      var field = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      return (T)field.GetValue(obj);
    }
    private static T GetPrivateInstanceProperty<T>(object obj, string name) {
      var field = obj.GetType().GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      return (T)field.GetValue(obj, null);
    }

    public Response Get(string path) {
      return Send(path);
    }

    public Response Post(string path, NameValueCollection data) {
      return Post(path, data);
    }

    public Response Post(string path, XHTMLr.Form data) {
      return Post(path, data);
    }

    public Response Post(string path, object data) {
      return Post(path, data);
    }

    public static NameValueCollection ConvertFromObject(object anonymous) {
      if (anonymous == null) return null;
      if (anonymous is string) return System.Web.HttpUtility.ParseQueryString(anonymous as string);
      if (anonymous is NameValueCollection) return anonymous as NameValueCollection;
      var form = new NameValueCollection();
      var dict = new RouteValueDictionary(anonymous);

      foreach (var kvp in dict) {
        if (kvp.Value.GetType().Name.Contains("Anonymous")) {
          var prefix = kvp.Key + ".";
          foreach (var innerkvp in new RouteValueDictionary(kvp.Value)) {
            form.Add(prefix + innerkvp.Key, innerkvp.Value.ToString());
          }
        } else {
          form.Add(kvp.Key, kvp.Value.ToString());
        }


      }
      return form;
    }

    public void Dispose() {

    }
  }

  [Serializable]
  public class SerializableCookie {
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public DateTime Expires { get; set; }
    public bool HttpOnly { get; set; }
    public string Path { get; set; }
    public NameValueCollection Values { get; set; }

    public SerializableCookie() { }
    public SerializableCookie(System.Web.HttpCookie cookie) {
      Name = cookie.Name;
      Value = cookie.Value;
      Domain = cookie.Domain;
      Expires = cookie.Expires;
      HttpOnly = cookie.HttpOnly;
      Path = cookie.Path;
      Values = cookie.Values;
    }

    public static SerializableCookie[] GetCookies(HttpCookieCollection cookies) {
      return cookies.AllKeys
         .Select(x => cookies[x])
         .Where(x => x != null)
         .Select(x => new SerializableCookie(x))
         .ToArray();
    }

    public static void Update(HttpCookieCollection cookies, SerializableCookie[] serializableCookies) {
      cookies.Clear();
      foreach (var cookie in serializableCookies)
        cookies.Set(cookie);
    }

    public static implicit operator System.Web.HttpCookie(SerializableCookie cookie) {
      var _cookie = new HttpCookie(cookie.Name, cookie.Value);
      _cookie.Domain = cookie.Domain;
      _cookie.Expires = cookie.Expires;
      _cookie.HttpOnly = cookie.HttpOnly;
      _cookie.Path = cookie.Path;
      cookie.Values.AllKeys.Select(x => _cookie.Values[x] = cookie.Values[x]);
      return _cookie;
    }
  }

  /// <summary>
  /// Use this to get data back to the test
  /// </summary>
  [Serializable]
  public class Response : MarshalByRefObject {
    public int StatusCode { get; set; }
    public string ResponseText { get; set; }

    internal SerializableCookie[] _SerializableCookies { get; set; }

    [NonSerialized]
  private  System.Web.HttpCookieCollection _Cookies;
    public System.Web.HttpCookieCollection Cookies {
      get {
        if (_Cookies == null) {
          _Cookies = new HttpCookieCollection();
          SerializableCookie.Update(_Cookies, _SerializableCookies);
        }
        return _Cookies;
      }
    }

    [NonSerialized]
    private System.Net.WebHeaderCollection _Headers;
    public System.Net.WebHeaderCollection Headers {
      get {
        if (_Headers == null) {
          _Headers = new System.Net.WebHeaderCollection();
          _Headers.Add(System.Web.HttpUtility.ParseQueryString(RawHeaders ?? string.Empty));
        }
        return _Headers;
      }
    }

    public string RawHeaders { get; set; }

    [NonSerialized]
    private System.Xml.Linq.XDocument _Xml;
    public System.Xml.Linq.XDocument ResponseXml {
      get {
        return _Xml = (_Xml = System.Xml.Linq.XDocument.Parse(XHTMLr.XHTML.ToXml(ResponseText)));
      }
    }

    public XHTMLr.Form[] GetForms() {
      return XHTMLr.Form.GetForms(ResponseXml);
    }
  }
}