using MfGames.Template;
using MfGames.Utility;
using System;
using System.IO;
using System.Xml;

/// <summary>
/// This tool precompiles a template and exports an output file that
/// contains the results suitable to be compiled or included in an
/// assembly.
/// </summary>
public class PrecompileTool
: Logable, ITool
{
#region Processing
    /// <summary>
    /// Executes the tool with the given parameters.
    /// </summary>
    public void Process(string [] args)
	{
		// Create a template factory
		TemplateFactory factory = new TemplateFactory();
		factory.TemporaryFile = outputFile;
		factory.DeleteTemporaryFile = false;
		factory.CompileAssembly = false;
		factory.ExtendClassName = extendclass;
		factory.NamespaceName = ns;
		factory.ClassName = classname;

		// Parse the file, create the file, but don't compile
		using (TextReader reader = templateFile.OpenText())
		{
			factory.Create(reader, templateFile.FullName);
		}
	}
#endregion

#region Properties
	private FileInfo templateFile;
	private FileInfo outputFile;
	private string ns = "TemporaryNamespace";
	private string classname = "TemporaryClass";
	private string extendclass = "MfGames.Template.TemplateBase";

	/// <summary>
	/// Sets and contains the name of the class to generate.
	/// </summary>
	[LongArgument("class", HasParameter = true)]
	public string ClassName
	{
		set { classname = value; }
	}

	/// <summary>
	/// Sets and contains the extend class to use.
	/// </summary>
	[LongArgument("extends", HasParameter = true)]
	public string ExtendsName
	{
		set { extendclass = value; }
	}

    /// <summary>
    /// Sets the output filename, from the command-line.
    /// </summary>
    [Positional(1, IsOptional = false,
			Name = "output",
			Description = "Output file to generate.")]
    public string OutputFilename
    {
		set
		{
			// Make sure the file exists
			outputFile = new FileInfo(value);
	
			if (outputFile.Exists)
			{
				throw new Exception("File exists: " + value);
			}
		}
    }

	/// <summary>
	/// Sets and contains the name of the namespace to generate.
	/// </summary>
	[LongArgument("namespace", HasParameter = true)]
	public string NamespaceName
	{
		set { ns = value; }
	}

    /// <summary>
    /// Sets the preference filename, which loads it into memory.
    /// </summary>
    [Positional(0, IsOptional = false,
			Name = "template",
			Description = "Template file to parse.")]
    public string TemplateFilename
    {
		set
		{
			// Make sure the file exists
			templateFile = new FileInfo(value);
	
			if (!templateFile.Exists)
			{
				throw new Exception("Cannot find file: " + value);
			}
		}
    }

	/// <summary>
	/// Returns a service description.
	/// </summary>
	public string Description
	{
		get { return "Precompiles a template into a C# or VB file."; }
	}

	/// <summary>
	/// Returns the name of the service, if there is one. This is
	/// typically the primary service name.
	/// </summary>
	public string ToolName
	{
		get { return "precompile"; }
	}
#endregion
}
