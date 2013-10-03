namespace Inceptum.Workflow
{
    public abstract class ActivityBase<TContext> : IActivity<TContext>
    {
        public abstract ActivityResult Execute(TContext context);

        public virtual ActivityResult Resume<TClosure>(TContext context, TClosure closure)
        {
            return ActivityResult.None;
        }
    }
}