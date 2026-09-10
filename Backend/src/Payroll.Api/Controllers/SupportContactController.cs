using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Common.Interfaces;
using Payroll.Shared;

namespace Payroll.Api.Controllers;

/// <summary>Public-within-the-app support contact details, sourced from SupportSettings config.</summary>
[Authorize(Policy = Constants.Policies.RequireEmployeeRole)]
[Route("api/support/contact")]
public class SupportContactController(ISupportNotificationSettings settings) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { email = settings.SupportEmail });
}
