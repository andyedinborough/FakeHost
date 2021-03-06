﻿using System;
using System.Net;
using System.Web;

namespace FakeHost.Browsing {
  /// <summary>
  /// Use this to get data back to the test
  /// </summary>
  [Serializable]
  public class Response : MarshalByRefObject {
    public int StatusCode { get; set; }
    public string ResponseText { get; set; }
    public Uri Url { get; set; }

    internal SerializableCookie[] _SerializableCookies { get; set; }

    [NonSerialized]
    private HttpCookieCollection _Cookies;
    public HttpCookieCollection Cookies {
      get {
        if (_Cookies == null) {
          _Cookies = new HttpCookieCollection();
          SerializableCookie.Update(_Cookies, _SerializableCookies);
        }
        return _Cookies;
      }
    }

    [NonSerialized]
    internal WebHeaderCollection _Headers;
    public WebHeaderCollection Headers {
      get {
        if (_Headers == null) {
          _Headers = new WebHeaderCollection();
          _Headers.Add(HttpUtility.ParseQueryString(RawHeaders ?? string.Empty));
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
      var forms = XHTMLr.Form.GetForms(ResponseXml);
      foreach (var form in forms) {
        var action = form.Action;
        if (string.IsNullOrEmpty(action))
          action = Url.ToString();
        var uri = new Uri(Url, action);
        form.Action = uri.ToString();
      }
      return forms;
    }
  }
}
