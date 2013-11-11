namespace Inceptum.Workflow
{
    public interface IActivityFactory 
    {
        TActivity Create<TActivity, TInput, TOutput>(params object[] activityCreationParams) where TActivity : IActivity<TInput, TOutput>;
    }
}