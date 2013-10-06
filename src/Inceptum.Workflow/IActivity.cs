using System;

namespace Inceptum.Workflow
{
    public interface IActivity<in TInput, out TOutput>
    {
        ActivityResult Execute(TInput input, Action<TOutput> processOutput);
        ActivityResult Resume<TClosure>(Action<TOutput> processOutput, TClosure closure);
    }
}