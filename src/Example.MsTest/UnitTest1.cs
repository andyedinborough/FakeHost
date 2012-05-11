using FakeHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Example.MsTest {
  /// <summary>
  /// Summary description for UnitTest1
  /// </summary>
  [TestClass]
  public class UnitTest1 {
    [TestMethod]
    public void TestMethod1() {
      var browser = new Browser();
      var result = browser.Get("/");
      Assert.AreEqual(200, result.StatusCode);
    }
  }
}
