using System;
using System.Collections.Generic;
using System.Text;

namespace ProjNet.CoordinateSystems.Projections
{
    /// <summary>
    /// A set of projection parameters
    /// </summary>
    // TODO: KeyedCollection<string, double>
    [Serializable] 
    public class ProjectionParameterSet : Dictionary<string, double>, IEquatable<ProjectionParameterSet>
    {
        private readonly Dictionary<string, string> _originalNames = new Dictionary<string, string>();
        private readonly Dictionary<int, string>  _originalIndex = new Dictionary<int, string>();
        /// <summary>
        /// Needed for serialzation
        /// </summary>
        public ProjectionParameterSet(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            :base(info, context)
        {}

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="parameters">An enumeration of parameters</param>
        public ProjectionParameterSet(IEnumerable<ProjectionParameter> parameters)
        {
            foreach (var pp in parameters)
            {
                string key = pp.Name.ToLowerInvariant();
                _originalNames.Add(key, pp.Name);
                _originalIndex.Add(_originalIndex.Count, key);
                Add(key, pp.Value);
            }
        }
        
        /// <summary>
        /// Function to create an enumeration of <see cref="ProjectionParameter"/>s of the content of this projection parameter set.
        /// </summary>
        /// <returns>An enumeration of <see cref="ProjectionParameter"/>s</returns>
        public IEnumerable<ProjectionParameter> ToProjectionParameter()
        {
            foreach (var oi in _originalIndex)
                yield return new ProjectionParameter(_originalNames[oi.Value], this[oi.Value]);
        }

        /// <summary>
        /// Function to get the value of a mandatory projection parameter
        /// </summary>
        /// <returns>The value of the parameter</returns>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="alternateNames">Possible alternate names for <paramref name="parameterName"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="parameterName"> or any of <paramref name="alternateNames"/> is not defined.</paramref></exception>
        public double GetParameterValue(string parameterName, params string[] alternateNames)
        {
            string name = parameterName.ToLowerInvariant();
            if (!ContainsKey(name))
            {
                foreach (string alternateName in alternateNames)
                {
                    double res;
                    if (TryGetValue(alternateName.ToLowerInvariant(), out res))
                        return res;
                }

                var sb = new StringBuilder();
                sb.AppendFormat("Missing projection parameter '{0}'", parameterName);
                if (alternateNames.Length > 0)
                {
                    sb.AppendFormat("\nIt is also not defined as '{0}'", alternateNames[0]);
                    for (int i = 1; i < alternateNames.Length; i++)
                        sb.AppendFormat(", '{0}'", alternateNames[i]);
                    sb.Append(".");
                }

                throw new ArgumentException(sb.ToString(), "parameterName");
            }
            return this[name];
        }

        /// <summary>
        /// Method to check if all mandatory projection parameters are passed
        /// </summary>
        public double GetOptionalParameterValue(string name, double value, params string[] alternateNames)
        {
            name = name.ToLowerInvariant();
            if (!ContainsKey(name))
            {
                foreach (string alternateName in alternateNames)
                {
                    double res;
                    if (TryGetValue(alternateName.ToLowerInvariant(), out res))
                        return res;
                }
                //Add(name, value);
                return value;
            }
            return this[name];
        }

        /// <summary>
        /// Function to find a parameter based on its name
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>The parameter if present, otherwise null</returns>
        public ProjectionParameter Find(string name)
        {
            name = name.ToLowerInvariant();
            return ContainsKey(name) ? new ProjectionParameter(_originalNames[name], this[name]) : null;
        }

        /// <summary>
        /// Function to get the parameter at the given index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The parameter</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ProjectionParameter GetAtIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            string name = _originalIndex[index];
            return new ProjectionParameter(_originalNames[name], this[name]);
        }

        /// <summary>
        /// Checks this projection parameter set with <paramref name="other"/>-
        /// </summary>
        /// <param name="other">The other projection parameter set.</param>
        /// <returns><value>true</value> if both sets are equal.</returns>
        public bool Equals(ProjectionParameterSet other)
        {
            if (other == null)
                return false;

            if (other.Count != Count)
                return false;

            foreach (var kvp in this)
            {
                if (!other.ContainsKey(kvp.Key))
                    return false;

                double otherValue = other.GetParameterValue(kvp.Key);
                if (otherValue != kvp.Value)
                    return false;
            }
            return true;
        }

        internal void SetParameterValue(string name, double value)
        {
            string key = name.ToLowerInvariant();
            if (!ContainsKey(key))
            {
                _originalIndex.Add(_originalIndex.Count, key);
                _originalNames.Add(key, name);
                Add(key, value);
            }
            else
            {
                Remove(key);
                Add(key, value);
            }
        }
    }
}
