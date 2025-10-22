using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace PQSoft.ReqNRoll.UnitTests;

[Binding]
public class WebAppTestSteps : PQSoft.ReqNRoll.ApiStepDefinitions
{
    public WebAppTestSteps(WebApplicationFactory<Program> factory) 
        : base(factory.CreateClient())
    {
    }
}
