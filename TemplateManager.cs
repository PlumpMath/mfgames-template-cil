namespace MfGames.Template
{
	using MfGames.Utility;
	using System.Collections;
	using System.IO;

	/// <summary>
	/// Implements the basic manager, which is easily extended, to
	/// manage templates. This is keyed off the filename, once
	/// requested, it will only compile the template once and store it.
	/// </summary>
	public class TemplateManager : Logable
	{
		// Contains the hash of templates
		private Hashtable templates = new Hashtable();

		// Contains the template factory
		private TemplateFactory factory = new TemplateFactory();

		/// <summary>
		/// Retrieves a template from the system, compiling it if needed.
		/// </summary>
		public ITemplate this[FileInfo file]
		{
			get
			{
				// Check the hash
				ITemplate template = (ITemplate) templates[file.FullName];

				if (template != null)
					return template;

				// Ignore blanks
				if (!file.Exists)
				{
					throw new TemplateException("File does not exist: "
						+ file);
				}

				// Create the template
				Debug("Parsing template: " + file);
				TextReader reader = file.OpenText();
				template = factory.Create(reader, file.ToString());
				reader.Close();

				// Save the template and return it
				templates[file.FullName] = template;
				return template;
			}
		}

		/// <summary>
		/// Contains the factory of this manager.
		/// </summary>
		public TemplateFactory Factory
		{
			get { return factory; }
			set
			{
				if (value == null)
					throw new TemplateException(
						"Cannot assign a null factory");

				factory = value;
				factory.TemplateManager = this;
			}
		}
	}
}
