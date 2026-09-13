namespace Template.MobileServer.Web.Application;

using Smart.Mapper;

using Template.MobileServer.Web.Models.Data;
using Template.MobileServer.Web.Models.Forms;

internal static partial class DataMapper
{
    [Mapper]
    public static partial DataForm ToForm(this DataEntity entity);

    [Mapper]
    public static partial DataResponse ToResponse(this DataEntity entity);
}
