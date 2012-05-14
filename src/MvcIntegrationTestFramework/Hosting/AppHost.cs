using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using FakeHost.Browsing;
using FakeHost.Interception;

namespace FakeHost.Hosting {
  /// <summary>
  /// Hosts an ASP.NET application within an ASP.NET-enabled .NET appdomain
  /// and provides methods for executing test code within that appdomain
  /// </summary>
  internal class AppHost {
    private readonly AppDomainProxy appDomainProxy; // The gateway to the ASP.NET-enabled .NET appdomain

    public AppHost(string appPhysicalDirectory)
      : this(appPhysicalDirectory, "/") {
    }

    public AppHost(string appPhysicalDirectory, string virtualDirectory) {
      appDomainProxy = (AppDomainProxy)ApplicationHost.CreateApplicationHost(typeof(AppDomainProxy), virtualDirectory, appPhysicalDirectory);

      appDomainProxy.RunCodeInAppDomain(() => {
        InitializeApplication();
        var filters = System.Web.Mvc.GlobalFilters.Filters;
        filters.Add(new InterceptionFilter(), int.MaxValue);
        LastRequestData.Reset();
      });
    }

    public void SimulateBrowsingSession(Action<BrowsingSession> testScript) {
      var serializableDelegate = new SerializableDelegate<Action<BrowsingSession>>(testScript);
      appDomainProxy.RunBrowsingSessionInAppDomain(serializableDelegate);
    }

    public void Execute(Action action) {
      var serializableDelegate = new SerializableDelegate<Action>(action);
      appDomainProxy.RunCodeInAppDomain(serializableDelegate);
    }

    #region Initializing app & interceptors
    private static void InitializeApplication() {
      var appInstance = GetApplicationInstance();
      appInstance.PostRequestHandlerExecute += delegate {
        // Collect references to context objects that would otherwise be lost
        // when the request is completed
        if (LastRequestData.HttpSessionState == null)
          LastRequestData.HttpSessionState = HttpContext.Current.Session;
        if (LastRequestData.Response == null)
          LastRequestData.Response = HttpContext.Current.Response;
      };
      RefreshEventsList(appInstance);

      RecycleApplicationInstance(appInstance);
    }

    #endregion

    #region Reflection hacks
    private static readonly MethodInfo getApplicationInstanceMethod;
    private static readonly MethodInfo recycleApplicationInstanceMethod;

    static AppHost() {
      // Get references to some MethodInfos we'll need to use later to bypass nonpublic access restrictions
      var httpApplicationFactory = typeof(HttpContext).Assembly.GetType("System.Web.HttpApplicationFactory", true);
      getApplicationInstanceMethod = httpApplicationFactory.GetMethod("GetApplicationInstance", BindingFlags.Static | BindingFlags.NonPublic);
      recycleApplicationInstanceMethod = httpApplicationFactory.GetMethod("RecycleApplicationInstance", BindingFlags.Static | BindingFlags.NonPublic);
    }

    private static HttpApplication GetApplicationInstance() {
      var writer = new StringWriter();
      var workerRequest = new SimpleWorkerRequest("", "", writer);
      var httpContext = new HttpContext(workerRequest);
      return (HttpApplication)getApplicationInstanceMethod.Invoke(null, new object[] { httpContext });
    }

    private static void RecycleApplicationInstance(HttpApplication appInstance) {
      recycleApplicationInstanceMethod.Invoke(null, new object[] { appInstance });
    }

    private static void RefreshEventsList(HttpApplication appInstance) {
      object stepManager = typeof(HttpApplication).GetField("_stepManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(appInstance);
      object resumeStepsWaitCallback = typeof(HttpApplication).GetField("_resumeStepsWaitCallback", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(appInstance);
      var buildStepsMethod = stepManager.GetType().GetMethod("BuildSteps", BindingFlags.NonPublic | BindingFlags.Instance);
      buildStepsMethod.Invoke(stepManager, new[] { resumeStepsWaitCallback });
    }

    #endregion
  }
}