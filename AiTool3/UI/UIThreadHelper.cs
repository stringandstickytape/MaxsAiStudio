public static class UIThreadHelper
{
    public static SynchronizationContext UISyncContext { get; private set; }

    public static void Initialize()
    {
        UISyncContext = SynchronizationContext.Current;
        if (UISyncContext == null)
        {
            throw new InvalidOperationException("SynchronizationContext.Current is null. Ensure Initialize is called on the UI thread.");
        }
    }

    public static bool IsOnUIThread()
    {
        return SynchronizationContext.Current == UISyncContext;
    }

    public static void ExecuteOnUIThread(Action action)
    {
        if (IsOnUIThread())
        {
            action();
        }
        else
        {
            UISyncContext.Post(_ => action(), null);
        }
    }
}