namespace Inceptum.Workflow
{
    public interface IActivityInputProvider
    {
        TInput GetInput<TInput>();
    }
}