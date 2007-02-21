namespace MfGames.Template
{
  using MfGames.Utility;
  using System.Collections;

  /// <summary>
  /// Defines the "context" or the situation where the template is
  /// run. Templates are compiled, then are requested to generate the
  /// results to a string, using the context object as the source.
  /// </summary>
  public class Context : Logable
  {
#region Properties
    // Contains the table of top-level properties
    private Hashtable props = new Hashtable();

    /// <summary>
    /// Defines the basic getter/setter for properties.
    /// </summary>
    public object this[string key]
    {
      get { return props[key]; }
      set { props[key] = value; }
    }
#endregion
  }
}
