using MvcIntegrationTestFramework;
using Xunit;

namespace Example.Xunit {
  public class SampleXunitTest {
    [Fact]
    public void Should_be_successful() {
      var browser = new Browser();
      var result = browser.Get("home/index");
      Assert.Equal(200, result.StatusCode);
    }
  }
}
