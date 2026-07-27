using Microsoft.AspNetCore.Mvc;
using Musiq.Models;
using Musiq.Services;

namespace Musiq.Controllers;

/// <summary>Controller subțire: doar validare, delegare către serviciu și redirect.</summary>
public class DownloadController : Controller
{
    private readonly IPlaylistDownloadService _downloadService;
    private readonly IToolLocator _toolLocator;

    public DownloadController(IPlaylistDownloadService downloadService, IToolLocator toolLocator)
    {
        _downloadService = downloadService;
        _toolLocator = toolLocator;
    }

    [HttpGet]
    public IActionResult Index() =>
        View(BuildIndexModel(new StartDownloadViewModel
        {
            OutputFolder = _downloadService.DefaultOutputFolder
        }));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(
        [Bind(Prefix = nameof(DownloadIndexViewModel.Form))] StartDownloadViewModel form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), BuildIndexModel(form));
        }

        try
        {
            var jobId = await _downloadService.QueueAsync(form, cancellationToken);
            return RedirectToAction(nameof(Status), new { id = jobId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Index), BuildIndexModel(form));
        }
    }

    [HttpGet]
    public IActionResult Status(Guid id)
    {
        var job = _downloadService.GetJob(id);

        return job is null ? NotFound() : View(job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(Guid id)
    {
        _downloadService.Cancel(id);

        return RedirectToAction(nameof(Status), new { id });
    }

    private DownloadIndexViewModel BuildIndexModel(StartDownloadViewModel form) => new()
    {
        Form = form,
        RecentJobs = _downloadService.GetRecentJobs(),
        Tools = _toolLocator.CheckAvailability()
    };
}
