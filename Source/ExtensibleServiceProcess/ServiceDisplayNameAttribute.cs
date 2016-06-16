using System;

namespace ExtensibleServiceProcess
{
    /// <summary>
    /// Specifies the service application display name. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ServiceDisplayNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDisplayNameAttribute"/> class.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        public ServiceDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; }
    }
}