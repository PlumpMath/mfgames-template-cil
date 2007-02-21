using MfGames.Utility;
using System;
using System.Collections;

/// <summary>
/// Implements a basic tool for manipulating templates.
/// </summary>
public class ToolEntry
: ToolManager
{
	/// <summary>
	/// Primary entry function into the system. This processes the
	/// commands, sets up the internal logging, and generates the
	/// various needed methods.
	/// </summary>
	public static void Main(string [] args)
	{
		// Create ourselve and start
		ToolEntry tool = new ToolEntry();
		tool.Process(args);
	}

	/// <summary>
	/// Registers the callbacks into the system using the various
	/// services.
	/// </summary>
	protected override void RegisterTools()
	{
		RegisterTool(new PrecompileTool());
	}

	/// <summary>
	/// Allows the various settings, such as the parameter formats, to
	/// the arguments before processing.
	/// </summary>
	protected override void SetArgumentParameters(CommandLineArguments args)
	{
		// Prepares the scanners
		args.Scanners.Add(LongArgumentScanner.DoubleDash);
	}
}
