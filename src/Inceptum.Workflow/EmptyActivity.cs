namespace Inceptum.Workflow
{
    public class EmptyActivity<TContext> : ActivityBase<TContext>
    {
        public override ActivityResult Execute(TContext context)
        {
            return ActivityResult.Succeeded;
        }
    }
}