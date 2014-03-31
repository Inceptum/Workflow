using System;

namespace Inceptum.Workflow
{
    public interface IActivityWithOutput<out TInput, out TOutput, out TFailOutput> where TInput:class where TOutput:class where TFailOutput:class
    {
        ActivityResult Resume<TClosure>(Guid activityExecutionId, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput, TClosure closure);
    }

    public interface IActivity<TInput, out TOutput, out TFailOutput> : IActivityWithOutput<TInput, TOutput, TFailOutput>
        where TInput : class where TOutput : class where TFailOutput : class
    {
        bool IsInputSerializable { get; }
        ActivityResult Execute(Guid activityExecutionId, TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput);
    }
}