using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inceptum.Workflow
{
    internal class YumlClassGenerator<TContext> : IWorkflowVisitor<TContext, string>
    {
        private readonly List<IGraphNode<TContext>> m_Visited = new List<IGraphNode<TContext>>();
        private readonly IDictionary<string, IGraphNode<TContext>> m_Nodes;

        public YumlClassGenerator(IDictionary<string, IGraphNode<TContext>> nodes)
        {
            m_Nodes = nodes;
        }


        public string Visit<TActivity, TInput, TOutput>(GraphNode<TContext, TActivity, TInput, TOutput> node) where TActivity : IActivity<TInput, TOutput>
        {
            return Visit(node as GraphNode<TContext>);
        }

        public string Visit(GraphNode<TContext> node)
        {
            m_Visited.Add(node);
            string res = "";
            foreach (var edge in node.Edges)
            {
                var nextNode = m_Nodes[edge.Node];
                res += string.Format("[{0}]{2}->[{1}]\n", node.Name, edge.Node, edge.Description ?? "");
                if (!m_Visited.Contains(nextNode))
                    res += nextNode.Accept(this);
            }
            return res;
        }
    }

    internal class YumlActivityGenerator<TContext> : IWorkflowVisitor<TContext, string>
    {
        private readonly Dictionary<char, string> m_Traslit = new Dictionary<char, string>()
                                                           {
                                                               {'�',"a"},{'�',"b"},{'�',"v"},{'�',"g"},{'�',"d"},{'�',"e"},{'�',"zh"},{'�',"z"},{'�',"i"},{'�',"y"},{'�',"k"},{'�',"l"},{'�',"m"},{'�',"n"},{'�',"o"},{'�',"p"},{'�',"r"},{'�',"s"},{'�',"t"},{'�',"u"},{'�',"f"},{'�',"h"},{'�',"c"},{'�',"ch"},{'�',"sh"},{'�',"shh"},{'�',""},{'�',"i"},{'�',""},{'�',"e"},{'�',"u"},{'�',"ya"},
                                                               {'�',"A"},{'�',"B"},{'�',"V"},{'�',"G"},{'�',"D"},{'�',"E"},{'�',"ZH"},{'�',"Z"},{'�',"I"},{'�',"Y"},{'�',"K"},{'�',"L"},{'�',"M"},{'�',"N"},{'�',"O"},{'�',"P"},{'�',"R"},{'�',"S"},{'�',"T"},{'�',"U"},{'�',"F"},{'�',"H"},{'�',"C"},{'�',"CH"},{'�',"SH"},{'�',"SHH"},{'�',""},{'�',"I"},{'�',""},{'�',"E"},{'�',"U"},{'�',"YA"},
                                                           };
        private readonly List<IGraphNode<TContext>> m_Visited = new List<IGraphNode<TContext>>();
        private readonly INodesResolver<TContext> m_Nodes;

        public YumlActivityGenerator(INodesResolver<TContext> nodes)
        {
            m_Nodes = nodes;
        }


        public string Visit<TActivity, TInput, TOutput>(GraphNode<TContext, TActivity, TInput, TOutput> node) where TActivity : IActivity<TInput, TOutput>
        {
            return Visit(node as GraphNode<TContext>);
        }

        public string Visit(GraphNode<TContext> node)
        {
            m_Visited.Add(node);
            string res = "";
            if (node.Edges.Count() > 1)
                res += string.Format("({0})-><{0} decision>", translit(node.Name));

            foreach (var edge in node.Edges)
            {
                var nextNode = m_Nodes[edge.Node];
                if (res != "") res += ",";
                res += string.Format("{0}{2}->{1}",
                    nodeStringFrom(node),
                    nodeStringTo(nextNode),
                    string.IsNullOrEmpty(edge.Description) || node.Edges.Count() == 1 ? "" : translit("[" + edge.Description + "]"));
                if (!m_Visited.Contains(nextNode))
                    res += "," + nextNode.Accept(this);
            }
            return res;
        }

        private string nodeStringTo(IGraphNode<TContext> node)
        {
            return string.Format("({0})", translit(node.Name));
        }
        private string nodeStringFrom(GraphNode<TContext> node) 
        {
            if (node.Edges.Count() > 1)
                return string.Format("<{0} decision>", translit(node.Name));
            return string.Format("({0})", translit(node.Name));
        }

        private string translit(string str)
        {
            return str.Aggregate(new StringBuilder(), (builder, c) => builder.Append(m_Traslit.ContainsKey(c) ? m_Traslit[c] : c.ToString()), builder => builder.ToString());

        }
    }
}