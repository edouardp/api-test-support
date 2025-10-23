using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace PQSoft.ReqNRoll.UnitTests;

[Binding]
public class WebAppTestSteps(WebApplicationFactory<Program> factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
