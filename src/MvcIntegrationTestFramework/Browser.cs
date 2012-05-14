using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Routing;
using FakeHost.Browsing;
using FakeHost.Hosting;

namespace FakeHost {
  public class Browser : IDisposable {
    private static object @lock = new object();
    private static AppHost _appHost;
    private static Uri _BaseUri;

    public Browser(string pathToYourWebProject = null, Uri baseUri = null) {
      Cookies = new HttpCookieCollection();
      AllowAutoRedirect = true;
      MaximumAutomaticRedirections = 15;
      InitializeAspNetRuntime(pathToYourWebProject, _BaseUri);
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
    public static void InitializeAspNetRuntime(string pathToYourWebProject = null, Uri baseUri = null) {
      if (_appHost == null)
        lock (@lock)
          if (_appHost == null) {
            _BaseUri = baseUri ?? new Uri("http://localhost/");
            if (pathToYourWebProject == null) {
              var guessDirectory = new DirectoryInfo(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..")));
              var projectDirs = guessDirectory.GetDirectories();
              foreach (var pd in projectDirs) {
                if (pd.GetFiles("web.config").Length == 1) {
                  pathToYourWebProject = pd.FullName;
                  break;
                }
              }
            }

            var ourDll0 = new Uri(typeof(FakeHost.Browser).Assembly.Location).LocalPath;
            var ourDll1 = Path.Combine(pathToYourWebProject, "bin", System.IO.Path.GetFileName(ourDll0));
            File.Copy(ourDll0, ourDll1, true);

            _appHost = new AppHost(pathToYourWebProject, _BaseUri.AbsolutePath);
          }
    }

    public HttpCookieCollection Cookies { get; internal set; }

    [NonSerialized]
    private System.Net.WebHeaderCollection _Headers = new System.Net.WebHeaderCollection();
    public System.Net.WebHeaderCollection Headers {
      get { return _Headers; }
    }

    public bool AllowAutoRedirect { get; set; }
    public int MaximumAutomaticRedirections { get; set; }

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
    protected Response Send(string url, object formData = null, string method = "GET") {
      var response = new Response();
      var formNameValueCollection = formData == null ? null : ConvertFromObject(formData);
      var headerCollection = System.Web.HttpUtility.ParseQueryString(string.Empty);
      foreach (string header in Headers) {
        headerCollection[header] = Headers[header];
      }

      var temp = new Uri(_BaseUri, url);
      Uri uri;
      var numRedirects = 0;
      do {
        var cookies = SerializableCookie.GetCookies(Cookies);
        lock (@lock)
          _appHost.SimulateBrowsingSession(browser => {
            SerializableCookie.Update(browser.Cookies, cookies);

            var result = browser.ProcessRequest(temp, numRedirects > 0 ? "GET" : method, formNameValueCollection, headerCollection);
            response.StatusCode = result.Response.StatusCode;
            response.ResponseText = result.ResponseText;
            response._SerializableCookies = SerializableCookie.GetCookies(browser.Cookies);

            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            var _customHeaders = GetPrivateInstanceField<object>(result.Response, "_customHeaders") as System.Collections.ArrayList
              ?? GetPrivateInstanceField<object>(result.Response, "_headers") as System.Collections.ArrayList;

            if (_customHeaders != null) {
              foreach (var hdr in _customHeaders) {
                var name = GetPrivateInstanceProperty<string>(hdr, "Name");
                var value = GetPrivateInstanceProperty<string>(hdr, "Value");
                query[name] = value;
              }
            }

            if (!string.IsNullOrEmpty(result.Response.RedirectLocation)) {
              query["Location"] = result.Response.RedirectLocation;
            }

            if (response.StatusCode == 200 && !string.IsNullOrEmpty(query["Location"]))
              response.StatusCode = 302;

            response.RawHeaders = query.ToString();
          });
        SerializableCookie.Update(Cookies, response._SerializableCookies);
        uri = temp;
        response._Headers = null;

      } while (
          AllowAutoRedirect
          && !string.IsNullOrEmpty(response.Headers["Location"])
          && (numRedirects++ < MaximumAutomaticRedirections)
          && (temp = new Uri(uri, response.Headers["Location"])).Host.Equals(_BaseUri.Host, StringComparison.InvariantCultureIgnoreCase)
        );

      response.Url = uri;
      return response;
    }

    /// <summary>
    /// Execute code in the ASP.NET AppDomain
    /// </summary>
    /// <param name="action"></param>
    public static void Execute(Action action) {
      _appHost.Execute(action);
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
      return Post(path, (object)data);
    }

    public Response Post(string path, XHTMLr.Form data) {
      return Post(path, (object)data);
    }
    public Response Post(XHTMLr.Form data) {
      return Post(data.Action, (object)data);
    }

    public Response Post(string path, object data) {
      return Send(path, data, "POST");
    }

    internal static NameValueCollection ConvertFromObject(object anonymous) {
      if (anonymous == null) return null;
      if (anonymous is string) return System.Web.HttpUtility.ParseQueryString(anonymous as string);
      var form = new NameValueCollection();

      if (anonymous is NameValueCollection) {
        //make a copy to ensure we have a class that is serializable
        var other = anonymous as NameValueCollection;
        foreach (var key in other.AllKeys)
          form[key] = other[key];
        return form;
      }

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
      //for future use
    }
  }
}