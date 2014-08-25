namespace Inceptum.Workflow
{
    public interface IActivityOutputProvider
    {
        TOutput GetOuput<TOutput>();
    }
}