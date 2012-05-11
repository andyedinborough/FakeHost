Integration testing ASP.NET MVC is messy. *FakeHost* aims to help.

The code has been forked and updated to make setup simpler:


```
[TestMethod]
public void TestLogin() {
  
  using (var browser = new Browser()) {
    var form = browser
      .Get("/account/logon")
      .GetForms()[0];


    form["email"] = "andy.edinborough@gmail.com";
    form["password"]  = "sUp3r1337&hX0rPr0of";

    var result = browser.Post("/account/logon", form);
      
    Assert.AreEqual(302, result.StatusCode);
  }

}
``` 