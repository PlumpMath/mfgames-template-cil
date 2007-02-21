using MfGames.Utility;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace MfGames.Template
{
    /// <summary>
    /// Creates templates by reading in either the string or the stream
    /// and generates the various code and components for the template,
    /// as wrapped in an ITemplate object.
    /// </summary>
    public class TemplateFactory : Logable
    {
        #region Constructions
        /// <summary>
        /// Constructs a template from a given stream. This uses the
        /// factory's properties to control the compliation process.
        /// </summary>
        public ITemplate Create(TextReader reader)
        {
            return Create(reader, null);
        }

        /// <summary>
        /// Constructs a template from a given stream. This uses the
        /// factory's properties to control the compliation process.
        /// </summary>
        public ITemplate Create(TextReader reader, string streamName)
        {
            // Set up the namespace, and the stream library
            CodeNamespace cns = CreateNamespace();
            CodeTypeDeclaration ctd = CreateClass();
            CodeMemberMethod emit = CreateTemplateMethod();

            cns.Types.Add(ctd);
            ctd.Members.Add(emit);

            // Parse the template
            CreateProperties(ctd);
            ParseTemplate(reader, streamName, cns, ctd, emit);

            // Compile the code
            ITemplate template = Compile(cns);

            // Return the base
            return template;
        }

        /// <summary>
        /// Creates a template out of the given string.
        /// </summary>
        public ITemplate Create(string templateString)
        {
            StringReader reader = new StringReader(templateString);
            ITemplate template = Create(reader);
            reader.Close();
            return template;
        }

        /// <summary>
        /// Constructs a template from a given template name. This
        /// uses the patterns registered with
        /// RegisterIncludePattern. If the pattern cannot be found, an
        /// exception is thrown.
        /// </summary>
        public ITemplate CreatePattern(string name)
        {
            // Go through the results
            foreach (string pattern in includePatterns)
            {
                string file = String.Format(pattern, name);
                Debug("CreatePattern: {0}", new FileInfo(file).FullName);

                if (File.Exists(file))
                {
                    // If we have a manager, create the template
                    // through it to cache the results.
                    if (TemplateManager != null)
                        return TemplateManager[new FileInfo(file)];
                    else
                        return Create(File.OpenText(file));
                }
            }

            // We couldn't find it
            throw new TemplateException("Cannot find template named: "
                + name);
        }

        /// <summary>
        /// Constructs the stream writer, which may or may not be
        /// temporary or in-memory.
        /// </summary>
        private StreamWriter CreateStreamWriter()
        {
            // Check for temporary file
            if (tempFile == null)
            {
                throw new TemplateException(
                    "Cannot create an in-memory stream");
            }
            else
            {
                // Create the stream
                Stream stream = tempFile.Open(FileMode.Create);
                return new StreamWriter(stream);
            }
        }
        #endregion

        #region Emitting
        /// <summary>
        /// Creates the extending class.
        /// </summary>
        private CodeTypeDeclaration CreateClass()
        {
            // Emit the starting class object
            CodeTypeDeclaration ctd = new CodeTypeDeclaration();
            ctd.IsClass = true;
            ctd.Name = className;
            ctd.TypeAttributes = TypeAttributes.Public;

            // Set up our extending class
            ctd.BaseTypes.Add(new CodeTypeReference(extendsName));

            // Return it
            return ctd;
        }

        /// <summary>
        /// Creates the namespace for this class.
        /// </summary>
        private CodeNamespace CreateNamespace()
        {
            // Initialize the namespace given
            CodeNamespace ns = new CodeNamespace(namespaceName);

            // Add our specific imports
            ns.Imports.Add(new CodeNamespaceImport("MfGames.Template"));
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("System.Text"));

            // Add the additional namespace
            foreach (string nsn in referencedNamespaces)
                ns.Imports.Add(new CodeNamespaceImport(nsn));

            return ns;
        }

        /// <summary>
        /// Constructs the core template method and returns it.
        /// </summary>
        private CodeMemberMethod CreateTemplateMethod()
        {
            // Emit the overriden method
            CodeMemberMethod emit = new CodeMemberMethod();
            emit.Name = "Write";
            emit.Attributes =
                MemberAttributes.Override | MemberAttributes.Public;

            // Set up the parameter
            CodeTypeReference refTextWriter =
                new CodeTypeReference("System.IO.TextWriter");
            CodeParameterDeclarationExpression declareTextWriter =
                new CodeParameterDeclarationExpression(refTextWriter,
                    "writer");

            emit.Parameters.Add(declareTextWriter);

            // Return it
            return emit;
        }
        #endregion

        #region Compliation
        private ArrayList referencedNamespaces = new ArrayList();
        private ArrayList referencedAssemblies = new ArrayList();

        /// <summary>
        /// Adds a referenced assembly.
        /// </summary>
        public void AddReferencedAssembly(Assembly assembly)
        {
            referencedAssemblies.Add(assembly);
        }

        /// <summary>
        /// Adds a referenced namespace into the code.
        /// </summary>
        public void AddReferencedNamespace(string ns)
        {
            referencedNamespaces.Add(ns);
        }

        /// <summary>
        /// Actually compiles the code, throwing an exception on the first
        /// error.
        /// </summary>
        private ITemplate Compile(CodeNamespace cns)
        {
            // Set up the provider and generator
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CodeGeneratorOptions gOptions = new CodeGeneratorOptions();
            CompilerParameters cOptions = new CompilerParameters();

            // Add our own assembly (using base) to make sure it can
            // be found. We also have to add the Utility library for
            // Developer Studio (thanks iceh). (Closes #51)
            cOptions.ReferencedAssemblies.Add(GetType().Assembly.Location);
            cOptions.ReferencedAssemblies
                .Add(typeof(Logger).Assembly.Location);

            // Add the other assemblies
            foreach (Assembly refass in referencedAssemblies)
            {
                cOptions.ReferencedAssemblies.Add(refass.Location);
            }

            // Set up other options
            cOptions.GenerateInMemory = true;

            // Figure out the stream
            CompilerResults results = null;

            if (tempFile == null)
            {
                // Create directory into memory
                CodeCompileUnit cu = new CodeCompileUnit();
                cu.Namespaces.Add(cns);

                if (compileAssembly)
                    results = provider.CompileAssemblyFromDom(cOptions, cu);
            }
            else
            {
                // Actually write out the code to the stream
                StreamWriter writer = CreateStreamWriter();
                provider.GenerateCodeFromNamespace(cns, writer, gOptions);
                writer.Close();

                // Now compile it from the file
                if (compileAssembly)
                {
                    results = provider.CompileAssemblyFromFile(cOptions,
                        tempFile.FullName);
                }
            }

            // If we aren't compiling, just return null
            if (!compileAssembly)
                return null;

            // Process the results
            foreach (CompilerError ce in results.Errors)
            {
                if (ce.IsWarning)
                {
                    Alert("{2} ({3}): {0}: {1}",
                        ce.ErrorNumber,
                        ce.ErrorText,
                        ce.FileName,
                        ce.Line);
                }
                else
                {
                    Error("{2} ({3}): {0}: {1}",
                        ce.ErrorNumber,
                        ce.ErrorText,
                        ce.FileName,
                        ce.Line);
                }
            }

            if (results.Errors.HasErrors)
                throw new TemplateException(
                    "The compiled template had errors");

            // Load the assembly
            Assembly assembly = results.CompiledAssembly;
            string typeName = namespaceName + "." + className;
            Type type = assembly.GetType(typeName);

            // Make sure we can create it and assign it
            if (type == null)
                throw new TemplateException("Cannot create temporary class "
                    + typeName);

            if (!typeof(ITemplate).IsAssignableFrom(type))
                throw new TemplateException(extendsName + " does not extend "
                    + "MfGames.Template.ITemplate");

            // Get the constructor
            ConstructorInfo ci = type.GetConstructor(new Type[] { });

            if (ci == null)
                throw new TemplateException(
                    "Cannot create a default constructor");

            // Create the object
            ITemplate template = (ITemplate) ci.Invoke(new object[] { });
            template.TemplateFactory = this;

            // At this point, everything is good, so delete the temp
            if (tempFile != null && deleteTempFile)
                tempFile.Delete();

            // Return it
            return template;
        }
        #endregion

        #region Including Templates
        private ArrayList includePatterns = new ArrayList();

        /// <summary>
        /// Registers a search pattern for inclusion via
        /// TemplateBase.Include(). This must have "{0}" as part of
        /// the string which is replaced with the template name given.
        /// </summary>
        public void RegisterIncludePattern(string pattern)
        {
            // Check for errors
            if (pattern == null ||
                pattern.Trim() == String.Empty ||
                pattern.IndexOf("{0}") < 0)
            {
                throw new TemplateException(
                    "The following was null, empty, or missing a "
                    + "\"{0}\" within it: " + pattern);
            }

            // Add it
            includePatterns.Add(pattern);
        }
        #endregion

        #region Parsing
        /// <summary>
        /// Parses the code fragment and creates the related CodeDom
        /// blocks.
        /// </summary>
        private void ParseCode(bool inCode, bool isEol, string buffer,
            CodeNamespace ns,
            CodeTypeDeclaration ctd,
            CodeMemberMethod emit,
            CodeLinePragma linePragma)
        {
            // We use this variable
            CodeVariableReferenceExpression wref =
                new CodeVariableReferenceExpression("writer");

            // Check for in-code
            if (inCode)
            {
                // Check the first character, which determines the mode
                if (buffer.StartsWith("="))
                {
                    // This is a "write variable out" command.
                    CodeSnippetExpression cse =
                        new CodeSnippetExpression(buffer.Substring(1));
                    CodeMethodInvokeExpression ivk =
                        new CodeMethodInvokeExpression(wref, "Write", cse);
                    CodeExpressionStatement ces =
                        new CodeExpressionStatement(ivk);
                    ces.LinePragma = linePragma;

                    // Add it
                    emit.Statements.Add(ivk);
                }
                else if (buffer.StartsWith("#"))
                {
                    // This is a outside of the parsing function code
                    CodeSnippetTypeMember cstm =
                        new CodeSnippetTypeMember(buffer.Substring(1));
                    ctd.Members.Add(cstm);
                }
                else if (buffer.StartsWith("@"))
                {
                    // Internal controls and configurations
                    ParseDirective(buffer.Substring(1).Trim(), ns, ctd);
                }
                else
                {
                    // Just parses it as code
                    CodeSnippetStatement css =
                        new CodeSnippetStatement(buffer);
                    css.LinePragma = linePragma;
                    emit.Statements.Add(css);
                }
            }
            else
            {
                // Ignore blanks, but not if at the end of the line
                if (buffer == "" && !isEol)
                    return;

                // Emit this as a straight text writer
                CodePrimitiveExpression buf =
                    new CodePrimitiveExpression(buffer);
                CodeMethodInvokeExpression ivk =
                    new CodeMethodInvokeExpression(wref,
                        (isEol) ? "WriteLine" : "Write",
                        buf);
                CodeExpressionStatement ces = new CodeExpressionStatement(ivk);
                ces.LinePragma = linePragma;

                // Add it
                emit.Statements.Add(ces);
            }
        }

        /// <summary>
        /// Performs the actual template parsing. This goes through the
        /// text reader and pulls out the blocks, or units, as broken
        /// apart by tags.
        /// </summary>
        private void ParseTemplate(TextReader reader,
            string contextName,
            CodeNamespace ns,
            CodeTypeDeclaration ctd,
            CodeMemberMethod emit)
        {
            // Read the template, line by line
            int count = 0;
            bool inCode = false;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                // Increment our line counter and emit it for debugging
                count++;
                CodeLinePragma linePragma =
                    new CodeLinePragma(contextName, count);

                // Continue processing the line until we are done with
                // it. We also use this loop to gather up the code
                // fragments into a single block.
                while (true)
                {
                    // Figure out the search token
                    string tag = inCode ? closeTag : openTag;

                    // Look for the tag we are searching for
                    int tagIndex = line.IndexOf(tag);

                    if (tagIndex < 0)
                    {
                        // Bug #60
                        // We didn't find the tag in the code. If we
                        // are inCode, then we just append the next
                        // line and try again, but if we are outside
                        // of the code block, we just process it.
                        if (inCode)
                        {
                            // Get the next line
                            string nextLine = reader.ReadLine();

                            if (nextLine != null)
                            {
                                // Bug #61, put in a newline
                                line += "\n" + nextLine;
                                continue;
                            }
                        }

                        // Finish up with the last builder
                        ParseCode(inCode, true, line,
                            ns, ctd, emit, linePragma);
                        break;
                    }

                    // If we have a tag, parse up to that bit.
                    string before = line.Substring(0, tagIndex);
                    ParseCode(inCode, false, before,
                        ns, ctd, emit, linePragma);

                    // Start the process anew
                    line = line.Substring(tagIndex + tag.Length);
                    inCode = !inCode;
                }
            }
        }
        #endregion

        #region Parsing Directives
        private static readonly Regex NamespaceValue =
            new Regex("namespace\\s*=\\s*\"(.*?)\"", RegexOptions.IgnoreCase);
        private static readonly Regex NameValue =
            new Regex("name\\s*=\\s*\"(.*?)\"", RegexOptions.IgnoreCase);
        private static readonly Regex TypeValue =
            new Regex("type\\s*=\\s*\"(.*?)\"", RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses the page directives and adds the results to the
        /// rest of the template.
        /// </summary>
        private void ParseDirective(string buffer,
            CodeNamespace ns,
            CodeTypeDeclaration ctd)
        {
            // Figure out the type
            int index = buffer.IndexOf(" ");

            if (index < 0)
                return;

            // Split out the directive
            string directive = buffer.Substring(0, index).Trim().ToLower();
            string remain = buffer.Substring(index).Trim();

            // Figure out the type
            if (directive == "template")
            {
                // Check for namespace
                if (NamespaceValue.IsMatch(remain))
                {
                    Match m = NamespaceValue.Match(remain);
                    ns.Imports.Add(new CodeNamespaceImport(m.Groups[1].Value));
                }
            }
            else if (directive == "variable")
            {
                // Create an internal variable
                string contextName = null;
                string propertyName = null;
                string typeName = null;

                // Pull out fields
                if (NameValue.IsMatch(remain))
                {
                    Match m = NameValue.Match(remain);
                    contextName = propertyName = m.Groups[1].Value.Trim();
                }

                if (TypeValue.IsMatch(remain))
                {
                    Match m = TypeValue.Match(remain);
                    typeName = m.Groups[1].Value.Trim();
                }
                else
                {
                    Error("Cannot find variable type: {0}/{1}",
                        propertyName, contextName);
                    return;
                }

                // Sanity checking
                if (propertyName == null || propertyName == "")
                    propertyName = contextName;

                if (propertyName == null || propertyName == "")
                {
                    Error("Cannot find variable name");
                    return;
                }

                // Create the variable
                CreateProperty(ctd, propertyName, contextName, typeName);
            }
            else
            {
                Alert("Unknown directive: {0}", directive);
            }
        }
        #endregion

        #region Properties
        private FileInfo tempFile = null;
        private bool deleteTempFile = true;
        private bool compileAssembly = true;
        private string openTag = "<%";
        private string closeTag = "%>";
        private string namespaceName = "TemporaryNamespace";
        private string className = "TemporaryClass";
        private string extendsName = "MfGames.Template.TemplateBase";
        private TemplateManager manager = null;

        /// <summary>
        /// Contains the name of the class to be generated.
        /// </summary>
        public string ClassName
        {
            get { return className; }
            set
            {
                if (value == null)
                    throw new TemplateException
                        ("Cannot assign a null class name");

                className = value.Trim();
            }
        }

        /// <summary>
        /// If this is true, then the assembly is compiled and
        /// returned from the Create() methods, otherwise null is
        /// returned.
        /// </summary>
        public bool CompileAssembly
        {
            get { return compileAssembly; }
            set { compileAssembly = value; }
        }

        /// <summary>
        /// If this is true (the default), then the temporary file is
        /// deleted if the parsing is successful.
        /// </summary>
        public bool DeleteTemporaryFile
        {
            get { return deleteTempFile; }
            set { deleteTempFile = value; }
        }

        /// <summary>
        /// Allows the change of the extending class name.
        /// </summary>
        public string ExtendClassName
        {
            get { return extendsName; }
            set { extendsName = value; }
        }

        /// <summary>
        /// Sets the namespace for the class.
        /// </summary>
        public string NamespaceName
        {
            get { return namespaceName; }
            set
            {
                if (value == null)
                    throw new TemplateException
                        ("Cannot assign a null namespace name");

                namespaceName = value.Trim();
            }
        }

        /// <summary>
        /// Contains the template manager used for this factory, for
        /// back-references and caching the Include() processing.
        /// </summary>
        public TemplateManager TemplateManager
        {
            get { return manager; }
            set { manager = value; }
        }

        /// <summary>
        /// If this is not null, then a temporary file is created (using
        /// the given object) before it is compiled into the assembly.
        /// </summary>
        public FileInfo TemporaryFile
        {
            get { return tempFile; }
            set { tempFile = value; }
        }
        #endregion

        #region Variable Registration
        private Hashtable properties = new Hashtable();

        /// <summary>
        /// Creates the various properties for this class.
        /// </summary>
        private void CreateProperties(CodeTypeDeclaration ctd)
        {
            // Go through the properties
            foreach (string propName in properties.Keys)
            {
                // Get our fields
                TemplateVariable var = (TemplateVariable) properties[propName];
                Type type = var.Type;
                string cname = var.ContextName;
                //Debug("Creating {0}: {1} (from {2}", propName, type,
                //cname);
                CreateProperty(ctd, propName, cname, type.Name);
            }
        }

        /// <summary>
        /// Constructs a single property in the template.
        /// </summary>
        private void CreateProperty(CodeTypeDeclaration ctd,
            string propertyName, string contextName, string typeName)
        {
            // Create the property
            CodeMemberProperty property = new CodeMemberProperty();
            property.Name = propertyName;
            property.Type = new CodeTypeReference(typeName);
            property.Attributes = MemberAttributes.Public;
            ctd.Members.Add(property);

            // Context variables
            CodeVariableReferenceExpression context =
                new CodeVariableReferenceExpression("Context");
            CodePrimitiveExpression contextEx =
                new CodePrimitiveExpression(contextName);
            CodeArrayIndexerExpression atContext =
                new CodeArrayIndexerExpression(context, contextEx);

            // Create the getter
            CodeCastExpression getCast =
                new CodeCastExpression(typeName, atContext);
            CodeMethodReturnStatement getReturn =
                new CodeMethodReturnStatement(getCast);
            property.GetStatements.Add(getReturn);

            // Create the setter
            CodeVariableReferenceExpression valueExpression =
                new CodeVariableReferenceExpression("value");
            CodeAssignStatement set =
                new CodeAssignStatement(atContext, valueExpression);
            property.SetStatements.Add(set);
        }

        /// <summary>
        /// Registers a variable (and a type) for the template. This is
        /// used to create local variables on the class that directly pull
        /// from the context. If a null type is given, it is removed.
        /// </summary>
        public void Register(string propertyName, Type type)
        {
            Register(propertyName, propertyName, type);
        }

        /// <summary>
        /// Registers a variable (and a type) for the template. This is
        /// used to create local variables on the class that directly pull
        /// from the context. If a null type is given, it is removed.
        /// </summary>
        public void Register(
            string propertyName, string contextName, Type type)
        {
            // Check for null
            if (type == null)
                properties.Remove(propertyName);

            // Create the entry
            TemplateVariable var = new TemplateVariable(contextName, type);
            properties[propertyName] = var;
        }
        #endregion
    }

    /// <summary>
    /// Simple structure to keep the context variables for generation.
    /// </summary>
    class TemplateVariable
    {
        public string ContextName = null;
        public Type Type = null;

        public TemplateVariable(string contextName, Type type)
        {
            ContextName = contextName;
            Type = type;
        }
    }
}
