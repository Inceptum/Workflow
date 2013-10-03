namespace Inceptum.Workflow
{
    public interface IActivity<in TContext>
    {
        ActivityResult Execute(TContext context);
        ActivityResult Resume<TClosure>(TContext context, TClosure closure);
    }
}