namespace Template.MobileServer.Web.Mappers;

using Smart.Mapper;

using Template.MobileServer.Web.Models.Data;
using Template.MobileServer.Web.Models.Forms;

internal static partial class DataMapper
{
    [Mapper]
    public static partial DataForm ToForm(DataEntity entity);

    [Mapper]
    public static partial DataResponse ToResponse(DataEntity entity);
}
