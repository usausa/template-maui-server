namespace Template.MobileServer.Web.Shared.Progress;

public static class ProgressStateExtensions
{
    public static ProgressStateScope Using(this ProgressState state) => new(state);

    public static ProgressStateScope Using(this ProgressState state, string message) => new(state, message);

    public static void Using(this ProgressState state, Action action)
    {
        using (new ProgressStateScope(state))
        {
            action();
        }
    }

    public static void Using(this ProgressState state, string message, Action action)
    {
        using (new ProgressStateScope(state, message))
        {
            action();
        }
    }

    public static T Using<T>(this ProgressState state, Func<T> func)
    {
        using (new ProgressStateScope(state))
        {
            return func();
        }
    }

    public static T Using<T>(this ProgressState state, string message, Func<T> func)
    {
        using (new ProgressStateScope(state, message))
        {
            return func();
        }
    }

    public static async ValueTask UsingAsync(this ProgressState state, Task func)
    {
        using (new ProgressStateScope(state))
        {
            await func;
        }
    }

    public static async ValueTask UsingAsync(this ProgressState state, string message, Task func)
    {
        using (new ProgressStateScope(state, message))
        {
            await func;
        }
    }

    public static async ValueTask UsingAsync(this ProgressState state, ValueTask func)
    {
        using (new ProgressStateScope(state))
        {
            await func;
        }
    }

    public static async ValueTask UsingAsync(this ProgressState state, string message, ValueTask func)
    {
        using (new ProgressStateScope(state, message))
        {
            await func;
        }
    }

    public static async ValueTask<T> UsingAsync<T>(this ProgressState state, Task<T> func)
    {
        using (new ProgressStateScope(state))
        {
            return await func;
        }
    }

    public static async ValueTask<T> UsingAsync<T>(this ProgressState state, string message, Task<T> func)
    {
        using (new ProgressStateScope(state, message))
        {
            return await func;
        }
    }

    public static async ValueTask<T> UsingAsync<T>(this ProgressState state, ValueTask<T> func)
    {
        using (new ProgressStateScope(state))
        {
            return await func;
        }
    }

    public static async ValueTask<T> UsingAsync<T>(this ProgressState state, string message, ValueTask<T> func)
    {
        using (new ProgressStateScope(state, message))
        {
            return await func;
        }
    }
}
