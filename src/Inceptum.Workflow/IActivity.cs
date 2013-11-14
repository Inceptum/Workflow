using System;

namespace Inceptum.Workflow
{
    public interface IActivity<in TInput, out TOutput, out TFailOutput>
    {
        ActivityResult Execute(TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput);
        ActivityResult Resume<TClosure>(Action<TOutput> processOutput, Action<TFailOutput> processFailOutput, TClosure closure);
    }
}