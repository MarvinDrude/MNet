
namespace MNet.Extensions;

internal static class TaskExtensions {

    public static void PipeFireAndForget(this Task task) {

        task?.ContinueWith(t => GC.KeepAlive(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

    }

}
