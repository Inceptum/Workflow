namespace Inceptum.Workflow
{
    public interface IActivityFactory 
    {
        TActivity Create<TActivity, TInput, TOutput,TFailOutput>(params object[] activityCreationParams) where TActivity : IActivity<TInput, TOutput,TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class;
    }
}