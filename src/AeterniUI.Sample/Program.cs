using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AeterniUI.Sample;
using AeterniUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAeterniUI(options =>
{
    options.Text.TimePickerHourLabel = "时";
    options.Text.TimePickerMinuteLabel = "分";
    options.Text.TimePickerSecondLabel = "秒";
    options.Text.TimePickerCancelText = "取消";
    options.Text.TimePickerConfirmText = "确定";
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
