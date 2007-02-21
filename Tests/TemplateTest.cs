using MfGames.Utility;
using MfGames.Template;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace MfGames.Template.Tests
{
    /// <summary>
    /// Automated unit tests for testing out the template system. This
    /// is intended to automatically stress most of the common
    /// functionality.
    /// </summary>
    [TestFixture]
    public class TemplateTest
        : Logable
    {
        #region Setup and Teardown
        private TemplateFactory factory = null;

        /// <summary>
        /// Constructs the test and sets up the internal tests.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Create the basic factory with defaults
            factory = new TemplateFactory();
            factory.RegisterIncludePattern(Path.Combine("Tests", "{0}.template"));
            factory.Register("Var1", "var1", typeof(string));
        }
        #endregion

        #region Simple Properties
        /// <summary>
        /// Tests pure in-memory template creation.
        /// </summary>
        [Test]
        public void InMemoryString()
        {
            factory.TemporaryFile = null;
            ITemplate it = factory.Create("This is a test");
            Context c = new Context();
            Assert.AreEqual("This is a test", it.ToString(c));
        }

        /// <summary>
        /// This test just handles the straight case of a string in, with no
        /// markup and results in the same string out.
        /// </summary>
        [Test]
        public void StraightString()
        {
            factory.TemporaryFile = new FileInfo("StraightString.cs");
            ITemplate it = factory.Create("This is a test");
            Context c = new Context();
            Assert.AreEqual("This is a test", it.ToString(c));
        }

        [Test]
        public void NewlineString()
        {
            factory.TemporaryFile = new FileInfo("NewlineString.cs");
            ITemplate it = factory.Create("This is\na test");
            Context c = new Context();
            Assert.AreEqual("This is" + Environment.NewLine + "a test",
                it.ToString(c));
        }

        [Test]
        public void OneArgString()
        {
            factory.TemporaryFile = new FileInfo("OneArgString.cs");
            ITemplate t = factory.Create("This is a <%=Context[\"var1\"]%> test");
            Context c = new Context();
            c["var1"] = "valid";
            Assert.AreEqual("This is a valid test", t.ToString(c));
        }

        [Test]
        public void OneArgVariableSetting()
        {
            factory.TemporaryFile = new FileInfo("OneArgVariableString.cs");
            ITemplate t =
                factory.Create("<%Var1=\"bob\";%>This is a <%=Var1%> test");
            Context c = new Context();
            c["var1"] = "valid";
            Assert.AreEqual("This is a bob test", t.ToString(c));
        }

        [Test]
        public void OneArgVariable()
        {
            factory.TemporaryFile = new FileInfo("OneArgVariable.cs");
            ITemplate t = factory.Create("This is a <%= Var1 %> test");
            Context c = new Context();
            c["var1"] = "valid";
            Assert.AreEqual("This is a valid test", t.ToString(c));
        }

        [Test]
        public void SimpleLoop()
        {
            factory.TemporaryFile = new FileInfo("SimpleLoop.cs");
            ITemplate t = factory.Create("<% for (int i = 1; i < 6; i++) { %>"
                + "<%=i%> "
                + "<% }%>");
            Context c = new Context();
            Assert.AreEqual("1 2 3 4 5 ", t.ToString(c));
        }

        [Test]
        public void ReuseTest()
        {
            factory.TemporaryFile = new FileInfo("ReuseTest.cs");
            ITemplate t = factory.Create("I am <%=Var1%>.");

            Context c1 = new Context();
            c1["var1"] = "okay";
            Assert.AreEqual("I am okay.", t.ToString(c1), "First context.");

            Context c2 = new Context();
            c2["var1"] = "good";
            Assert.AreEqual("I am good.", t.ToString(c2), "Second context.");
        }
        #endregion

        #region Including Templates
        [Test]
        public void SimpleInclude()
        {
            factory.TemporaryFile = new FileInfo("SimpleInclude.cs");
            ITemplate t = factory.Create("<%= Include(\"inner\") %>");

            Context c1 = new Context();
            c1["var1"] = "template";
            Assert.AreEqual("inner(template)", t.ToString(c1));
        }

        [Test]
        public void SimpleInclude2()
        {
            factory.TemporaryFile = new FileInfo("SimpleInclude.cs");
            ITemplate t = factory.Create("<%= Include(\"inner\") %>"
                                         + "<% Var1 = \"second\"; %>"
                                         + "<%= Include(\"inner\") %>");

            Context c1 = new Context();
            c1["var1"] = "template";
            Assert.AreEqual("inner(template)inner(second)", t.ToString(c1));
        }
        #endregion

        #region External Functions
        [Test]
        public void TestExternal()
        {
            factory.TemporaryFile = new FileInfo("External.cs");
            ITemplate t = factory.Create(
                "<%# private string Foo() { return \"bar\"; } %>"
                + "<%=Foo()%>");

            Context c1 = new Context();
            Assert.AreEqual("bar", t.ToString(c1));
        }

        [Test]
        public void TestExternal60()
        {
            factory.TemporaryFile = new FileInfo("External.cs");
            ITemplate t = factory.Create(
                "<%# private string Foo()\n"
                + "  {\n"
                + "    return \"bar\";\n"
                + "  }\n"
                + "%>"
                + "<%=Foo()%>");

            Context c1 = new Context();
            Assert.AreEqual("bar", t.ToString(c1));
        }
        #endregion

        #region Directives
        [Test]
        public void TestNamespace()
        {
            factory.TemporaryFile = new FileInfo("TestNamespace.cs");
            ITemplate t = factory.Create(
                "<%@ Template Namespace=\"System.Collections\"%>"
                + "<%=typeof(ArrayList)%>");

            Context c1 = new Context();
            Assert.AreEqual("System.Collections.ArrayList", t.ToString(c1));
        }

        [Test]
        public void TestVariable()
        {
            factory.TemporaryFile = new FileInfo("TestVariable.cs");
            ITemplate t = factory.Create(
                "<%@ Variable Name=\"Var2\" Type=\"System.String\" %>"
                + "<%=Var2%>");

            Context c1 = new Context();
            c1["Var2"] = "yes";
            Assert.AreEqual("yes", t.ToString(c1));
        }
        #endregion

        #region Bugs
        [Test]
        public void TestBug60()
        {
            factory.TemporaryFile = new FileInfo("TestBug60.cs");
            ITemplate t = factory.Create("<%=\nVar1\n%>");

            Context c1 = new Context();
            c1["var1"] = "template";
            Assert.AreEqual("template", t.ToString(c1));
        }

        [Test]
        public void TestBug60a()
        {
            factory.TemporaryFile = new FileInfo("TestBug60a.cs");
            ITemplate t = factory.Create("<%\nVar1\n =\n \"inner\";\n%>"
                + "<%= Var1 %>");

            Context c1 = new Context();
            Assert.AreEqual("inner", t.ToString(c1));
        }

        [Test]
        public void TestBug61()
        {
            factory.TemporaryFile = new FileInfo("TestBug61.cs");
            ITemplate t = factory.Create("<%=\n//Comment\nVar1\n%>");

            Context c1 = new Context();
            c1["var1"] = "template";
            Assert.AreEqual("template", t.ToString(c1));
        }

        [Test]
        public void TestBug61a()
        {
            factory.TemporaryFile = new FileInfo("TestBug61a.cs");
            ITemplate t = factory.Create
                ("<%\n//Comment\nVar1 = \"bob\";\n"
                    + "// Comment\nVar1 = \"template\"; %><%=Var1%>");

            Context c1 = new Context();
            Assert.AreEqual("template", t.ToString(c1));
        }

        [Test]
        public void TestBug61b()
        {
            factory.TemporaryFile = new FileInfo("TestBug61b.cs");
            ITemplate t = factory.Create
                ("<%= Foo() %><%#\n//Comment\n"
                    + "string Foo() { return \"template\"; } %>");

            Context c1 = new Context();
            Assert.AreEqual("template", t.ToString(c1));
        }
        #endregion
    }
}