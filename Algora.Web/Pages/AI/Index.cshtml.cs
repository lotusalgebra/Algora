using Algora.Application.DTOs.AI;
using Algora.Application.Interfaces.AI;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.AI;

public class IndexModel : PageModel
{
    private readonly IAiContentService _aiContentService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IAiContentService aiContentService,
        ILogger<IndexModel> logger)
    {
        _aiContentService = aiContentService;
        _logger = logger;
    }

    public List<AiProviderInfo> Providers { get; set; } = new();
    public int DescriptionsGenerated { get; set; }
    public int SeoOptimized { get; set; }
    public int ChatbotMessages { get; set; }

    public void OnGet()
    {
        try
        {
            Providers = _aiContentService.GetAvailableTextProviders().ToList();

            // TODO: Load actual stats from database
            DescriptionsGenerated = 0;
            SeoOptimized = 0;
            ChatbotMessages = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AI dashboard");
        }
    }
}
