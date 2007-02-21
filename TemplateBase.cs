using MfGames.Utility;
using System;
using System.IO;

namespace MfGames.Template
{
    /// <summary>
    /// This is the base template object, which is used as the abstract
    /// base for the templates. This class is extended, via code, to
    /// include the automatic context retrieval functions, if
    /// requested.
    /// </summary>
    public abstract class TemplateBase
    : Logable, ITemplate
    {
        /// <summary>
        /// Applies the template using the context, then returns the
        /// results as a string.
        /// </summary>
        public string ToString(Context context)
        {
            // Change the context
            StringWriter sw = new StringWriter();
            Write(context, sw);
            sw.Close();

            // Trim off the final \n (and/or \r, artifact of the process)
            string buffer = sw.ToString();
            return buffer.Substring(0, buffer.Length - Environment.NewLine.Length);
        }

        /// <summary>
        /// Writes out a template with a given context.
        /// </summary>
        public abstract void Write(TextWriter writer);

        /// <summary>
        /// Writes out the template to a given stream.
        /// </summary>
        public void Write(Context context, TextWriter writer)
        {
            this.context = context;
            Write(writer);
        }

        #region Including Templates
        /// <summary>
        /// Returns the contents of another template as a
        /// string. This uses the factory's RegisterIncludePattern to
        /// identify the actual name (allowing for search paths).
        /// </summary>
        protected virtual string Include(string templateName)
        {
            // Get the template
            ITemplate template = factory.CreatePattern(templateName);
            return template.ToString(Context);
        }
        #endregion

        #region Properties
        private Context context = null;
        private TemplateFactory factory = null;

        /// <summary>
        /// Contains the context for this template.
        /// </summary>
        protected Context Context
        {
            get { return context; }
        }

        /// <summary>
        /// Contains a reference to the template factory that
        /// constructed this template. The template factory is
        /// automatically set before the template is returned to the
        /// user.
        /// </summary>
        public TemplateFactory TemplateFactory
        {
            get { return factory; }
            set { factory = value; }
        }
        #endregion
    }
}
