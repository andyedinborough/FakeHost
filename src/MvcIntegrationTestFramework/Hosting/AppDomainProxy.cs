using System;
using System.Web;
using FakeHost.Browsing;

namespace FakeHost.Hosting {
  /// <summary>
  /// Simply provides a remoting gateway to execute code within the ASP.NET-hosting appdomain
  /// </summary>
  internal class AppDomainProxy : MarshalByRefObject, IDisposable {
    public void RunCodeInAppDomain(Action codeToRun) {
      codeToRun();
    }

    public void RunCodeInAppDomain(SerializableDelegate<Action> script) {
      script.Delegate();
    }

    public void RunBrowsingSessionInAppDomain(SerializableDelegate<Action<BrowsingSession>> script) {
      var browsingSession = new BrowsingSession();
      script.Delegate(browsingSession);
    }

    public override object InitializeLifetimeService() {
      return null; // Tells .NET not to expire this remoting object
    }

    public void Dispose()
    {
      HttpRuntime.Close();
    }
  }
}