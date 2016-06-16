using System;

namespace ExtensibleServiceProcess
{
    /// <summary>
    /// Specifies the service application name. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ServiceNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ServiceNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }
    }
}