namespace Inceptum.Workflow
{
    public interface IWorkflowPersister<TContext>
    {
        void Save(TContext context, Execution<TContext> execution);
        Execution<TContext> Load(TContext context);
    }
}