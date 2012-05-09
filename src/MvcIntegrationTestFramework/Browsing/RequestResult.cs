using System.Web;
using System.Web.Mvc;

namespace MvcIntegrationTestFramework.Browsing {
  /// <summary>
  /// Represents the result of a simulated request
  /// </summary>
  internal class RequestResult {
    public HttpResponse Response { get; set; }
    public string ResponseText { get; set; }
    public ActionExecutedContext ActionExecutedContext { get; set; }
    public ResultExecutedContext ResultExecutedContext { get; set; }
  }
}