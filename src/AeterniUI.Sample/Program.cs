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

    // 库的默认文案是英文、由宿主本地化；示例站整体是中文，所以这里把浮层与确认的可见文案补齐，
    // 否则同一页里会同时出现「取消」和「Cancel」，读者只会把它当成缺陷而不是"默认值"。
    options.Text.ConfirmAcceptLabel = "确定";
    options.Text.ConfirmCancelLabel = "取消";
    options.Text.DialogCloseLabel = "关闭对话框";
    options.Text.AlertCloseLabel = "关闭提示";
    options.Text.ToastCloseLabel = "关闭通知";
    options.Text.DrawerLabel = "面板";
    options.Text.DrawerCloseLabel = "关闭面板";
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 页头浮层的共享状态：触发元素在页头的玻璃卡片里、浮层渲染在卡片之外，两半是靠这些模型绑在
// 一起的，所以生命周期跟随应用（scoped），不能由某个组件各自持有。
builder.Services.AddScoped<AeterniUI.Sample.Components.Showcase.ShowcaseSearchModel>();
builder.Services.AddScoped<AeterniUI.Sample.Components.Showcase.ShowcaseSettingsModel>();
builder.Services.AddScoped<AeterniUI.Sample.Components.Showcase.ShowcasePreviewModel>();

await builder.Build().RunAsync();
