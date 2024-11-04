using Blazored.LocalStorage;
using Client;
using Client.ApplicationStates;
using ClientCommon.Helpers;
using ClientLibrary.Helpers;
using ClientLibrary.Services.Contracts;
using ClientLibrary.Services.Implementations;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1JpRGJGfV5ycEVCal9STnJeUj0eQnxTdEFiWH9acXJWRWRdVkV3Ww==");

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CustomHttpHandler>();
builder.Services.AddHttpClient("SystemApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7254");
}).AddHttpMessageHandler<CustomHttpHandler>();

builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<GetHttpClient>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAutheticationStateProvider>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

builder.Services.AddScoped<DepartmentState>();

builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<Syncfusion.Blazor.Popups.SfDialogService>();

await builder.Build().RunAsync();
