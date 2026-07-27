using Musiq.Models;
using Musiq.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurare unelte externe (yt-dlp / ffmpeg).
builder.Services.Configure<YtDlpOptions>(builder.Configuration.GetSection(YtDlpOptions.SectionName));

// Servicii de descărcare.
builder.Services.AddSingleton<IToolLocator, ToolLocator>();
builder.Services.AddSingleton<IDownloadJobStore, InMemoryDownloadJobStore>();
builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
builder.Services.AddSingleton<IYtDlpService, YtDlpService>();
builder.Services.AddSingleton<IPlaylistDownloadService, PlaylistDownloadService>();
builder.Services.AddHostedService<DownloadWorker>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Download}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
