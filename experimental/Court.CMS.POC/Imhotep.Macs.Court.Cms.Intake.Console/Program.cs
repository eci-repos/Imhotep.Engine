
using Imhotep.Macs.Api.Controllers;
using Imhotep.Macs.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Register MACS Controllers and Intake Service (SRV-INTAKE-01)
// Explicitly tell ASP.NET Core to look for controllers in the MACS Class Library
builder.Services.AddControllers()
    .AddApplicationPart(typeof(CaseSubmissionController).Assembly);

// REPAIR: Override default ASP.NET Core validation to return 422 Unprocessable Entity for VAL-001
builder.Services.AddControllers()
    .AddApplicationPart(typeof(CaseSubmissionController).Assembly)
    .ConfigureApiBehaviorOptions(options =>
    {
       options.InvalidModelStateResponseFactory = context =>
       {
          return new UnprocessableEntityObjectResult(context.ModelState);
       };
    });

builder.Services.AddScoped<IIntakeRestService, IntakeRestService>();

// 2. POL-NIST-001: Configure Entra ID OAuth Authentication 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
       // In a real environment, these come from your appsettings.json
       options.Authority = "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0";
       options.Audience = "YOUR_CLIENT_ID";
    });

var app = builder.Build();

// 3. Enforce the Zero Trust Access pipeline
app.UseAuthentication();
app.UseAuthorization();

// 4. Map the INT-001 Case Submission API routes
app.MapControllers();

// Start the local .NET MACS runtime environment (INF-001)
app.Run();
