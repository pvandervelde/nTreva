//-----------------------------------------------------------------------
// <copyright company="NTreva">
//     Copyright 2013 NTreva. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using NTreva.Properties;

namespace Tools
{
    /// <summary>
    /// Defines the resource properties for the tools library.
    /// </summary>
    internal static class ToolsResources
    {
        /// <summary>
        /// Gets the string that is used when an exception is thrown because the template could not be loaded.
        /// </summary>
        public static string CouldNotLoadTemplateExceptionMessage
        {
            get
            {
                return Resources.Exceptions_Messages_CouldNotLoadTemplate;
            }
        }
    }
}
