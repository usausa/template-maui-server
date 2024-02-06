namespace Template.MobileServer.Backend.Web.Controllers;

using Template.MobileServer.Backend.Services;
using Template.MobileServer.Backend.Web;

public class DataController : BaseApiController
{
    private IMapper Mapper { get; }

    private DataService DataService { get; }

    public DataController(
        IMapper mapper,
        DataService dataService)
    {
        Mapper = mapper;
        DataService = dataService;
    }

    [HttpGet]
    public async ValueTask<IActionResult> List()
    {
        return Ok(new DataListResponse
        {
            Entries = (await DataService.QueryListAsync()).Select(x => Mapper.Map<DataEntity, DataListResponseEntry>(x)!).ToArray()
        });
    }
}
