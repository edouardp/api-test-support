using Microsoft.AspNetCore.Mvc.Testing;
using PQSoft.ReqNRoll;
using Reqnroll;

namespace PQSoft.ReqNRoll.UnitTests;

[Binding]
public class WebAppTestSteps : ApiStepDefinitions
{
    public WebAppTestSteps(WebApplicationFactory<Program> factory) 
        : base(factory.CreateClient())
    {
    }
}
