namespace Inceptum.Workflow
{
    public interface IActivityFactory 
    {
        TActivity Create<TActivity, TInput, TOutput>() where TActivity : IActivity<TInput, TOutput>;
    }
}