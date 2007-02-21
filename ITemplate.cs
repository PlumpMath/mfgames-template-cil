namespace MfGames.Template
{
	using System.IO;

	/// <summary>
	/// Basic API for a constructed template. The TemplateFactory class
	/// creates an object that extends this interface, but the
	/// underlying template is not "public".
	/// </summary>
	public interface ITemplate
	{
		/// <summary>
		/// Contains the factory that constructed this template.
		/// </summary>
		TemplateFactory TemplateFactory { set; get; }

		/// <summary>
		/// Applies the template using the context, then returns the
		/// results as a string.
		/// </summary>
		string ToString(Context context);

		/// <summary>
		/// Writes out the template results to the given text writer.
		/// </summary>
		void Write(Context context, TextWriter writer);
	}
}
