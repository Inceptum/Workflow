using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using Inceptum.Workflow.Fluent;
using NUnit.Framework;

namespace Inceptum.Workflow.Tests
{
    [TestFixture]
    public class WorkflowTests
    {
        class TestActivity1 : TestActivity { }
        class TestActivity2 : TestActivity { }
        class TestActivity3 : TestActivity { }
        class FailingTestActivity : TestActivity
        {
            public override ActivityResult Execute(List<string> context)
            {
                base.Execute(context);
                return ActivityResult.Failed;
            }
        }

        class AsyncTestActivity : TestActivity
        {
            public override ActivityResult Resume<TClosure>(List<string> context, TClosure closure)
            {
                return ActivityResult.Succeeded;
            }

            public override ActivityResult Execute(List<string> context)
            {
                base.Execute(context);
                return ActivityResult.Pending;
            }
        }
        class TestActivity : ActivityBase<List<string>>
        {

            public override ActivityResult Execute(List<string> context)
            {
                context.Add(GetType().Name);
                return ActivityResult.Succeeded;
            }
        }


        class ValidateInputData : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                return ActivityResult.Succeeded;
            }
        }


        class GenerateConfirmationDocument : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                return ActivityResult.Succeeded;
            }
        }
        class CreateDiasDocument : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                Console.WriteLine("\tDias doc requested");
                return ActivityResult.Pending;
            }

            public override ActivityResult Resume<TClosure>(Executable context, TClosure closure)
            {
                if (closure is int && (int)(object)closure > 0)
                {
                    Console.WriteLine("\tDias doc is received. Number is #" + closure);
                    context.DiasDoc = closure.ToString();
                    return ActivityResult.Succeeded;
                }

                Console.WriteLine("\tDias doc is not received");
                return ActivityResult.Pending;

            }
        }

        class CardDebit : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                return ActivityResult.Succeeded;
            }
        }


        class CardSettlement : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                return ActivityResult.Succeeded;
            }
        }


        class GenerateProofDocument : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                return ActivityResult.Failed;
            }
        }

        class NotefyAdmins : ActivityBase<Executable>
        {
            public override ActivityResult Execute(Executable context)
            {
                return ActivityResult.Succeeded;
            }
        }


        [Test]
        [Ignore]
        public void Test3()
        {
            var wf = new Workflow<Executable>("", new InMemoryPersister<Executable>());
            wf.Configure(cfg => cfg.Do<ValidateInputData>("Проверка входных данных").Do<GenerateConfirmationDocument>("Генерация документа на подпись")
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


        [Test]
        [ExpectedException(typeof(ConfigurationErrorsException), ExpectedMessage = "Node 'node1' references unknown node 'Not existing node'")]
        public void InvalidEdgeValidationTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg.Do<TestActivity1>("node1").ContinueWith("Not existing node"));

        }

        [Test]
        [ExpectedException(typeof(ConfigurationErrorsException), ExpectedMessage = "Node 'node1' is not connected with any other node.")]
        public void NotTerminatingNodeHasNoEdgesValidationTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg =>
                                cfg.Do<TestActivity1>("node1")
                                   .WithBranch().Do<TestActivity2>("node2").End());

        }


        [Test]
        public void StraightExecutionFlowTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg.Do<TestActivity1>("node1").Do<TestActivity2>("node2").End());
            var context = new List<string>();
            var execution = wf.Run(context);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(context, Is.EquivalentTo(new[] { "TestActivity1", "TestActivity2" }), "Wrong activities were executed");

        }

        [Test]
        public void ExecutionFlowWithContextConditionsTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            int branchSelector = 0;
            wf.Configure(cfg => cfg.Do<TestActivity1>("node1")
                                        .On("constraint1").DeterminedAs(list => branchSelector == 0).ContinueWith("node2")
                                        .On("constraint2").DeterminedAs(list => branchSelector == 1).End()
                                    .WithBranch().Do<TestActivity2>("node2").End()
                                    .WithBranch().Do<TestActivity3>("node3").End());
            var context0 = new List<string>();
            var execution1 = wf.Run(context0);
            var context1 = new List<string>();
            branchSelector = 1;
            var execution2 = wf.Run(context1);
            Assert.That(execution1.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(execution2.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(context0, Is.EquivalentTo(new[] { "TestActivity1", "TestActivity2" }), "Wrong activities were executed");
            Assert.That(context1, Is.EquivalentTo(new[] { "TestActivity1" }), "Wrong activities were executed");
        }

        [Test]
        public void ExecutionFlowWithContextConditionAsFirstStepTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            int branchSelector = 0;
            wf.Configure(cfg => cfg.On("constraint1").DeterminedAs(list => branchSelector == 0).ContinueWith("node2")
                                        .On("constraint2").DeterminedAs(list => branchSelector == 1).End()
                                    .WithBranch().Do<TestActivity2>("node2").End()
                                    .WithBranch().Do<TestActivity3>("node3").End());
            var context0 = new List<string>();
            var execution1 = wf.Run(context0);
            var context1 = new List<string>();
            branchSelector = 1;
            var execution2 = wf.Run(context1);
            Assert.That(execution1.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(execution2.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(context0, Is.EquivalentTo(new[] { "TestActivity2" }), "Wrong activities were executed");
            Assert.That(context1, Is.EquivalentTo(new string[0]), "Wrong activities were executed");
        }

        [Test]
        public void OnFailTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg.Do<FailingTestActivity>("node1").OnFail("node2")
                                    .WithBranch().Do<TestActivity2>("node2").End());
            var context = new List<string>();
            var execution = wf.Run(context);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete));
            Assert.That(context, Is.EquivalentTo(new[] { "FailingTestActivity", "TestActivity2" }));
        }


        [Test]
        public void ResumeTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg.Do<TestActivity1>("node1").Do<AsyncTestActivity>("node2").Do<TestActivity3>("node3").End());
            var context = new List<string>();
            var execution = wf.Run(context);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.InProgress), "Execution was not paused when async activity returned Pednding status");
            Assert.That(context, Is.EquivalentTo(new[] { "TestActivity1", "AsyncTestActivity" }), "Wrong activities were executed");

            execution = wf.Resume(context, new { message = "йа - кложурка!!!" });
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete), "Execution was not complete after async activity was successfully resumed and returned Succeeded status");
            Assert.That(context, Is.EquivalentTo(new[] { "TestActivity1", "AsyncTestActivity", "TestActivity3" }), "Wrong activities were executed");

        }


        [Test]
        public void ResumeFromTest()
        {
            var wf = new Workflow<List<string>>("", new InMemoryPersister<List<string>>());
            wf.Configure(cfg => cfg.Do<TestActivity1>("node1").Do<AsyncTestActivity>("node2").Do<TestActivity3>("node3").Do<TestActivity2>("node4").End());
            var context = new List<string>();
            var execution = wf.Run(context);
            Assert.That(execution.State, Is.EqualTo(WorkflowState.InProgress), "Execution was not paused when async activity returned Pednding status");
            Assert.That(context, Is.EquivalentTo(new[] { "TestActivity1", "AsyncTestActivity" }), "Wrong activities were executed");

            execution = wf.ResumeFrom(context, "node4");
            Assert.That(execution.State, Is.EqualTo(WorkflowState.Complete), "Execution was not complete after async activity was successfully resumed and returned Succeeded status");
            Assert.That(context, Is.EquivalentTo(new[] { "TestActivity1", "AsyncTestActivity", "TestActivity2" }), "Wrong activities were executed");

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
