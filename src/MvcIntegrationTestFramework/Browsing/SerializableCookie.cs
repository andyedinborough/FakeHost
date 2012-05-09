using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;

namespace MvcIntegrationTestFramework.Browsing {
  [Serializable]
  internal class SerializableCookie {
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
}
