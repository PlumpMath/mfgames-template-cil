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
}
