namespace Template.MobileServer.Web.Components;

using MudBlazor;

public static class SnackbarExtensions
{
    public static Snackbar AddInfo(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, Severity.Info);

    public static Snackbar AddSuccess(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, Severity.Success);

    public static Snackbar AddWarning(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, Severity.Warning);

    public static Snackbar AddError(this ISnackbar snackbar, string message) =>
        snackbar.Add(message, Severity.Error);
}
