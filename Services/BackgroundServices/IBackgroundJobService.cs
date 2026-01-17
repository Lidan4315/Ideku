namespace Ideku.Services.BackgroundServices
{
    /// Service untuk execute tasks di background dengan proper scope management
    public interface IBackgroundJobService
    {
        /// Execute async action in background (fire-and-forget)
        /// <param name="jobName">Job name untuk logging dan monitoring</param>
        /// <param name="action">Action yang akan dijalankan dengan scoped service provider</param>
        void ExecuteInBackground(string jobName, Func<IServiceProvider, Task> action);
    }
}
