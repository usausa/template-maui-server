namespace Template.MobileServer.Backend.Web.Application;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DataEntity, DataListResponseEntry>();
    }
}
