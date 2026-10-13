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

// 页头浮层的共享状态：触发元素在页头的玻璃卡片里、浮层渲染在卡片之外，两半是靠这些模型绑在
// 一起的，所以生命周期跟随应用（scoped），不能由某个组件各自持有。
builder.Services.AddScoped<AeterniUI.Sample.Components.Showcase.ShowcaseSearchModel>();
builder.Services.AddScoped<AeterniUI.Sample.Components.Showcase.ShowcaseSettingsModel>();

await builder.Build().RunAsync();
