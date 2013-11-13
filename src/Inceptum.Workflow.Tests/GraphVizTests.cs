using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Inceptum.Graphviz;
using NUnit.Framework;

namespace Inceptum.Workflow.Tests
{
    [TestFixture]
    public class GraphVizTests
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [Test]
        public void GeneratePngTest()
        {

            foreach (var file in Directory.GetFiles(@"w:\GRAPHVIZ\graphviz\bin\", "*.dll"))
            {
                LoadLibrary(file);
            }
       /*     LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\cdt.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\cgraph.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\Pathplan.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\ltdl.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\libexpat.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\zlib1.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\gvc.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\gvplugin_core.dll");
            LoadLibrary(@"w:\GRAPHVIZ\graphviz\bin\gvplugin_dot_layout.dll");*/
            var src = @"
digraph {
start [label=start]
start->node1
node1 [label=node1]
node1->node2
node2 [label=node2]
node2->end
end [label=end]

}

";
            var image = Inceptum.Graphviz.GraphViz.RenderImage(src);
            image.Save(@"w:\GRAPHVIZ\test.png",ImageFormat.Png);
        }
         
    }
}