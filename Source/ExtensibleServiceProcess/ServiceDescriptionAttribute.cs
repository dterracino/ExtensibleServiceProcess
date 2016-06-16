using System;

namespace ExtensibleServiceProcess
{
    /// <summary>
    /// Specifies the service application description. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ServiceDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDescriptionAttribute"/> class.
        /// </summary>
        /// <param name="description">The description.</param>
        public ServiceDescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; }
    }
}