using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using Inceptum.Workflow;
using Inceptum.Workflow.Fluent;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Inceptum.Workflow.Tests
{
    [TestFixture]
    public class WorkflowTests
    {
        private class TestActivity1 : TestActivity
        {
        }

        private class TestActivity2 : TestActivity
        {
        }

        private class TestActivity3 : TestActivity
        {
        }

        private class FailingTestActivity : TestActivity
        {
            public override ActivityResult Execute(List<string> input, Action<List<string>> processOutput)
            {
                return ActivityResult.Failed;
            }
        }

        private class AsyncTestActivity : TestActivity
        {
            public override ActivityResult Execute(List<string> input, Action<List<string>> processOutput)
            {
                return ActivityResult.Pending;
            }

            public override ActivityResult Resume<TClosure>(Action<List<string>> processOutput, TClosure closure)
            {
                return ActivityResult.Succeeded;
            }

        }

        private class TestActivity : ActivityBase<List<string>, List<string>>
        {

            public ActivityResult Execute(List<string> context)
            {
                context.Add(GetType().Name);
                return ActivityResult.Succeeded;
            }

            public override ActivityResult Execute(List<string> input, Action<List<string>> processOutput)
            {
                processOutput(new List<string>(input.ToArray().Reverse().ToArray()));
                return ActivityResult.Succeeded;
            }
        }


        private class ValidateInputData : ActivityBase<Executable, Executable>
        {
            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                processOutput(input);
                return ActivityResult.Succeeded;
            }
        }


        private class GenerateConfirmationDocument : ActivityBase<Executable, Executable>
        {
            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                return ActivityResult.Succeeded;
            }
        }

        private class CreateDiasDocument : ActivityBase<Executable, Executable>
        {
            private Executable m_Input;

            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                m_Input = input;
                Console.WriteLine("\tDias doc requested");
                return ActivityResult.Pending;
            }

            public override ActivityResult Resume<TClosure>(Action<Executable> processOutput, TClosure closure)
            {
                if (closure is int && (int) (object) closure > 0)
                {
                    Console.WriteLine("\tDias doc is received. Number is #" + closure);
//context.DiasDoc = closure.ToString();
                    return ActivityResult.Succeeded;
                }

                Console.WriteLine("\tDias doc is not received");
                return ActivityResult.Pending;

            }
        }

        private class CardDebit : ActivityBase<Executable, Executable>
        {
            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                return ActivityResult.Succeeded;
            }
        }


        private class CardSettlement : ActivityBase<Executable, Executable>
        {
            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                return ActivityResult.Succeeded;
            }
        }


        private class GenerateProofDocument : ActivityBase<Executable, Executable>
        {
            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                return ActivityResult.Failed;
            }
        }

        private class NotefyAdmins : ActivityBase<Executable, Executable>
        {
            public override ActivityResult Execute(Executable input, Action<Executable> processOutput)
            {
                return ActivityResult.Succeeded;
            }
        }

/*

        [Test]
        [Ignore]
        public void Test3()
        {
            var wf = new Workflow<Executable>("", new InMemoryPersister<Executable>());
            wf.Configure(cfg => cfg.Do<ValidateInputData>("Проверка входных данных", executable1 => null, executable1 => { }).Do<GenerateConfirmationDocument>("Генерация документа на подпись")
                                    .On("Карточный счет").DeterminedAs(operation => operation.AccountType == "card").ContinueWith("Списание с карты")
                                    .On("Текущий   счет").DeterminedAs(operation => operation.AccountType == "acc").ContinueWith("Обращение в диас")
                                .WithBranch().Do<GenerateProofDocument>("Генерация документа").OnFail("Уведомить админов").End()
                                .WithBranch().Do<CardDebit>("Списание с карты").Do<CreateDiasDocument>("исполнение документа в диасофт").Do<CardSettlement>("создание проводки в 3card").OnFail("Уведомить админов").ContinueWith("Генерация документа")
                                .WithBranch().Do<CreateDiasDocument>("Обращение в диас").ContinueWith("Генерация документа")
                                .WithBranch().Do<NotefyAdmins>("Уведомить админов").End()
                                );

            var executable = new Executable { AccountType = "card" };
            wf.Run(executable);
            Console.WriteLine();
            Console.WriteLine();
            wf.Resume(executable, -1);
            Console.WriteLine();
            Console.WriteLine();
            wf.Resume(executable, 1);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(wf);


        }
  
        [Test]
        [Ignore]
        public void Test4()
        {
            var wf = new Workflow<Executable>("", new InMemoryPersister<Executable>());
            wf.Configure(cfg => cfg.Do("ValidateInputData","Проверка входных данных",
                                        ex =>new {ex.DiasDoc,ex.AccountType},
                                        (ex, values) => ex.DiasDoc=values.Test)
                                    .Do<GenerateConfirmationDocument>("Генерация документа на подпись")
                                    .On("Карточный счет").DeterminedAs(operation => operation.AccountType == "card").ContinueWith("Списание с карты")
                                    .On("Текущий   счет").DeterminedAs(operation => operation.AccountType == "acc").ContinueWith("Обращение в диас")
                                .WithBranch().Do<GenerateProofDocument>("Генерация документа").OnFail("Уведомить админов").End()
                                .WithBranch().Do<CardDebit>("Списание с карты").Do<CreateDiasDocument>("исполнение документа в диасофт").Do<CardSettlement>("создание проводки в 3card").OnFail("Уведомить админов").ContinueWith("Генерация документа")
                                .WithBranch().Do<CreateDiasDocument>("Обращение в диас").ContinueWith("Генерация документа")
                                .WithBranch().Do<NotefyAdmins>("Уведомить админов").End()
                                );

            var executable = new Executable { AccountType = "card" };
            wf.Run(executable);
            Console.WriteLine();
            Console.WriteLine();
            wf.Resume(executable, -1);
            Console.WriteLine();
            Console.WriteLine();
            wf.Resume(executable, 1);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(wf);


        }
*/


        [Test]
        [ExpectedException(typeof (ConfigurationErrorsException), ExpectedMessage = "Node 'node1' references unknown node 'Not existing node'")]
        public void InvalidEdgeValidationTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(
                cfg => cfg.Do<TestActivity1, List<string>, List<string>>("node1", list => list, (input, output) => { }).ContinueWith("Not existing node"));

        }

        [Test]
        [ExpectedException(typeof (ConfigurationErrorsException), ExpectedMessage = "Node 'node1' is not connected with any other node.")]
        public void NotTerminatingNodeHasNoEdgesValidationTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg =>
                cfg.Do<TestActivity1, List<string>, List<string>>("node1", list => list, (input, output) => { })
                    .WithBranch().Do<TestActivity2, List<string>, List<string>>("node2", list => list, (input, output) => { }).End());

        }


        [Test]
        public void StraightExecutionFlowTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg
                .Do<TestActivity1, List<string>, List<string>>("node1", list => list, (context, output) => context.Add("TestActivity1"))
                .Do<TestActivity2, List<string>, List<string>>("node2", list => list, (context, output) => context.Add("TestActivity2"))
                .End());
            var wfContext = new List<string>();
            var execution = wf.Run(wfContext);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(wfContext, Is.EquivalentTo(new[] {"TestActivity1", "TestActivity2"}), "Wrong activities were executed");

        }

        [Test]
        public void WorkflowCorruptsOnActivityFailWithoutExplicitFailBranchTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg
                .Do<TestActivity1, List<string>, List<string>>("node1", list => list, (context, output) => context.Add("TestActivity1"))
                .Do<FailingTestActivity, List<string>, List<string>>("node2", list => list, (context, output) => context.Add("TestActivity2"))
                .End());
            var wfContext = new List<string>();
            var execution = wf.Run(wfContext);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Corrupted));
        }

        [Test]
        public void ExplicitFailBranchTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg
                .Do<TestActivity1, List<string>, List<string>>("node1", list => list, (context, output) => context.Add("TestActivity1"))
                .Do<TestActivity2, List<string>, List<string>>("node2", list => list, (context, output) => context.Add("TestActivity2"))
                .Fail());
            var wfContext = new List<string>();
            var execution = wf.Run(wfContext);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Failed));
            Assert.That(wfContext, Is.EquivalentTo(new[] { "TestActivity1", "TestActivity2" }), "Wrong activities were executed");
        }

        [Test]
        public void ExecutionFlowWithContextConditionsTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            int branchSelector = 0;
            wf.Configure(cfg => cfg.Do<TestActivity1, List<string>, List<string>>("node1", list => list, (context, output) => context.Add("TestActivity1"))
                .On("constraint1").DeterminedAs(list => branchSelector == 0).ContinueWith("node2")
                .On("constraint2").DeterminedAs(list => branchSelector == 1).End()
                .WithBranch().Do<TestActivity2, List<string>, List<string>>("node2", list => list, (context, output) => context.Add("TestActivity2")).End()
                .WithBranch().Do<TestActivity3, List<string>, List<string>>("node3", list => list, (context, output) => context.Add("TestActivity3")).End());
            var context0 = new List<string>();
            var execution1 = wf.Run(context0);
            var context1 = new List<string>();
            branchSelector = 1;
            var execution2 = wf.Run(context1);
            Assert.That(execution1.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(execution2.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(context0, Is.EquivalentTo(new[] {"TestActivity1", "TestActivity2"}), "Wrong activities were executed");
            Assert.That(context1, Is.EquivalentTo(new[] {"TestActivity1"}), "Wrong activities were executed");
        }

        [Test]
        public void ExecutionFlowWithContextConditionAsFirstStepTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            int branchSelector = 0;
            wf.Configure(cfg => cfg.On("constraint1").DeterminedAs(list => branchSelector == 0).ContinueWith("node2")
                .On("constraint2").DeterminedAs(list => branchSelector == 1).End()
                .WithBranch().Do<TestActivity2, List<string>, List<string>>("node2", list => list, (context, output) => context.Add("TestActivity2")).End()
                .WithBranch().Do<TestActivity3, List<string>, List<string>>("node3", list => list, (context, output) => context.Add("TestActivity3")).End());
            var context0 = new List<string>();
            var execution1 = wf.Run(context0);
            var context1 = new List<string>();
            branchSelector = 1;
            var execution2 = wf.Run(context1);
            Assert.That(execution1.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(execution2.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(context0, Is.EquivalentTo(new[] {"TestActivity2"}), "Wrong activities were executed");
            Assert.That(context1, Is.EquivalentTo(new string[0]), "Wrong activities were executed");
        }

        [Test]
        public void OnFailTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg.Do<FailingTestActivity, List<string>, List<string>>("node1", list =>
            {
                list.Add("FailingTestActivity");
                return list;
            }, (context, output) => { }).OnFail("node2")
                .WithBranch().Do<TestActivity2, List<string>, List<string>>("node2", list => list, (context, output) => context.Add("TestActivity2")).End());
            var wfContext = new List<string>();
            var execution = wf.Run(wfContext);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(wfContext, Is.EquivalentTo(new[] {"FailingTestActivity", "TestActivity2"}));
        }


        [Test]
        public void ResumeTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg
                .Do<TestActivity1, List<string>, List<string>>("node1", list => list, (context, output) => context.Add("TestActivity1"))
                .Do<AsyncTestActivity, List<string>, List<string>>("node2", list =>
                {
                    list.Add("AsyncTestActivity");
                    return list;
                }, (context, output) => { })
                .Do<TestActivity3, List<string>, List<string>>("node3", list => list, (context, output) => context.Add("TestActivity3"))
                .End());
            var wfContext = new List<string>();
            var execution = wf.Run(wfContext);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.InProgress), "Execution was not paused when async activity returned Pednding status");
            Assert.That(wfContext, Is.EquivalentTo(new[] {"TestActivity1", "AsyncTestActivity"}), "Wrong activities were executed");

            execution = wf.Resume(wfContext, new {message = "йа - кложурка!!!"});
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete),
                "Execution was not complete after async activity was successfully resumed and returned Succeeded status");
            Assert.That(wfContext, Is.EquivalentTo(new[] {"TestActivity1", "AsyncTestActivity", "TestActivity3"}), "Wrong activities were executed");

        }

        [Test]
        public void ResumeFromTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
/*
                    wf.Configure(cfg => cfg.Do("Проверка входных данных").Do("Генерация документа на подпись")
                                 .On("Карточный счет").ContinueWith("Списание с карты")
                                 .On("Текущий   счет").ContinueWith("Обращение в диас")
                             .WithBranch().Do("Генерация документа").OnFail("Уведомить админов").End()
                             .WithBranch().Do("Списание с карты").Do("исполнение документа в диасофт").Do("создание проводки в 3card").OnFail("Уведомить админов").ContinueWith("Генерация документа")
                             .WithBranch().Do("Обращение в диас").ContinueWith("Генерация документа")
                             .WithBranch().Do("Уведомить админов").End(),
                             Node.Named("Проверка входных данных").Takes(List => list).ProcessResult((o, r) => o.Add("TestActivity1")).With<TestActivity>()
                             );*/
            wf.Configure(cfg => cfg
                .Do<TestActivity1, List<string>, List<string>>("node1", list => list, (context, output) => context.Add("TestActivity1"))
                .Do<AsyncTestActivity, List<string>, List<string>>("node2", list =>
                {
                    list.Add("AsyncTestActivity");
                    return list;
                }, (context, output) => { })
                .Do<TestActivity3, List<string>, List<string>>("node3", list => list, (context, output) => context.Add("TestActivity3"))
                .Do<TestActivity1, List<string>, List<string>>("node4", list => list, (context, output) => context.Add("TestActivity2"))
                .End());
            var wfContext = new List<string>();
            var execution = wf.Run(wfContext);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.InProgress), "Execution was not paused when async activity returned Pednding status");
            Assert.That(wfContext, Is.EquivalentTo(new[] {"TestActivity1", "AsyncTestActivity"}), "Wrong activities were executed");

            execution = wf.ResumeFrom(wfContext, "node4");
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete),
                "Execution was not complete after async activity was successfully resumed and returned Succeeded status");
            Assert.That(wfContext, Is.EquivalentTo(new[] {"TestActivity1", "AsyncTestActivity", "TestActivity2"}), "Wrong activities were executed");

        }

        [Test]
        public void GenericActivityTest()
        {
          
            FakeExecutor executor = new FakeExecutor();

            var wf = new Workflow<dynamic>("", new InMemoryPersister<dynamic>(), activityExecutor: executor);
            wf.Configure(cfg => cfg
                .Do("CardPayActivity", "Pay", o => new {o.CardNumber}, (o, output) => o.IsPayed = output.IsPayed)
                .Do("SendSmsActivity", "SendSMS", o => new {o.Phone}, (o, output) => o.IsSmsSent = output.IsSmsSent)
                .End());
            dynamic wfContext = new JObject();
            wfContext.CardNumber = "1234 2546 7897 4566";
            wfContext.Phone = "+79265603326";
            wfContext.IsPayed = false;
            wfContext.IsSmsSent = false;

            var execution = wf.Run(wfContext);
            wf.Resume(wfContext, new ActivityState {NodeName = "Pay", Status = ActivityResult.Succeeded, Values = JObject.Parse("{'IsPayed':true}")});
            wf.Resume(wfContext, new ActivityState {NodeName = "SendSMS", Status = ActivityResult.Succeeded, Values = JObject.Parse("{'IsSmsSent':true}")});
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(wfContext.IsPayed == true, Is.True, "Wrong activities were executed");
            Assert.That(wfContext.IsSmsSent == true, Is.True, "Wrong activities were executed");
            Assert.That(executor.Input.Count, Is.EqualTo(2), "Executor was called with wrong data");

        }
    }

    public class FakeExecutor : IActivityExecutor
    {
        public List<object> Input=new List<object>();
        public ActivityResult Execute(string activityType, string nodeName, dynamic input, Action<dynamic> processOutput)
        {
            Input.Add(input);
            return ActivityResult.Pending;
        }
    }


    public class InMemoryPersister<TContext> : IWorkflowPersister<TContext>
    {
        readonly Dictionary<TContext, Execution<TContext>> m_Cahce = new Dictionary<TContext, Execution<TContext>>();
        public void Save(TContext context, Execution<TContext> execution)
        {
            if (!m_Cahce.ContainsValue(execution))
                m_Cahce.Add(context, execution);
        }

        public Execution<TContext> Load(TContext context)
        {
            return m_Cahce[context];
        }
    }

    class Executable
    {
        public string AccountType { get; set; }
        public string DiasDoc { get; set; }
    }

}
