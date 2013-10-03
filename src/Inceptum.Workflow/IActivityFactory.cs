namespace Inceptum.Workflow
{
    public interface IActivityFactory<out TContext>
    {
        TActivity Create<TActivity>() where TActivity : IActivity<TContext>;
    }
}