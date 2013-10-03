namespace Inceptum.Workflow
{
    public class EndActivity<TContext> : ActivityBase<TContext>
    {
        public override ActivityResult Execute(TContext context)
        {
            return ActivityResult.Succeeded;
        }
    }
}