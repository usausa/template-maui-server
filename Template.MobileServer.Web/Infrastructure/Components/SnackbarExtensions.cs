namespace Template.MobileServer.Web.Infrastructure.Components;

public static class SnackbarExtensions
{
    public static Snackbar? AddInfo(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, MudBlazor.Severity.Info);

    public static Snackbar? AddSuccess(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, MudBlazor.Severity.Success);

    public static Snackbar? AddWarning(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, MudBlazor.Severity.Warning);

    public static Snackbar? AddError(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, MudBlazor.Severity.Error);
}
