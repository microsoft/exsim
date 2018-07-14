// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.ComponentModel;

using UR.Graphing;

namespace MSModel
{
    public delegate void RecalibrationDelegate(Target target);

    public abstract class Profile : ICloneable
    {
        /// <summary>
        /// Creates an instance of profile type T (as a derived type if the "type" attribute is set).
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <typeparam name="T">The profile base class (e.g. Hardware).</typeparam>
        /// <param name="element">The XML element to deserialize.</param>
        /// <param name="parent">The parent profile (if any).</param>
        /// <returns></returns>
        public static T CreateInstance<T>(XElement element, T parent) where T : Profile, new()
        {
            T instance;

            // 
            // If the "type" attribute is set, use its value to extract the type to be instantiated.  
            // Otherwise, allocate using the base class type directly.  
            //
            // This is designed to allow derived classes of a given profile type to be instantiated.
            //

            if (element.Attribute("type") != null)
            {
                Type type = Assembly.GetExecutingAssembly().GetType(element.Attribute("type").Value);

                if (type == null)
                {
                    throw new ArgumentException(String.Format("An invalid type was specified: {0}", element.Attribute("type")));
                }

                instance = (T)Activator.CreateInstance(type);
            }
            else
            {
                instance = new T();
            }

            instance.FromXml(element, parent);

            return instance;
        }

        public Profile()
        {
            this.HasProperties = new HashSet<string>();
            this.DefaultPropertyValues = new Dictionary<string, string>();
            this.Guid = System.Guid.NewGuid();
            this.Features = new List<Profile>();
        }

        public Profile(XElement element, Profile parent)
            : this()
        {
            FromXml(element, parent);
        }

        public virtual object Clone()
        {
            Profile p = (Profile)this.MemberwiseClone();

            p.HasProperties = new HashSet<string>(this.HasProperties);
            p.DefaultPropertyValues = new Dictionary<string, string>(this.DefaultPropertyValues);
            p.Features = new List<Profile>(this.Features);
            p.cachedProperties = null; // reset cached properties as it needs to be refreshed when we clone.

            return p;
        }

        public override string ToString()
        {
            return this.FullSymbol;
        }

        protected virtual void Initialize()
        {
        }

        public virtual void FromXml(XElement element, Profile parent)
        {
            Initialize();

            if (element.Attribute("name") != null)
            {
                this.Name = element.Attribute("name").Value;
            }

            if (element.Attribute("symbol") != null)
            {
                this.Symbol = element.Attribute("symbol").Value;
            }

            if (element.Attribute("guid") != null)
            {
                this.Guid = Guid.Parse(element.Attribute("guid").Value);
            }

            if (element.Attribute("alias") != null)
            {
                this.Alias = element.Attribute("alias").Value;
            }
            else
            {
                this.Alias = this.Symbol;
            }

            if (element.Element("Example") != null)
            {
                this.Example = element.Element("Example").Value;
            }

            if (element.Attribute("hidden") != null && Convert.ToBoolean(element.Attribute("hidden").Value) == true)
            {
                this.Hidden = true;
            }

            //
            // Inherit.
            //

            this.Parent = parent;
            this.HasProperties = new HashSet<string>();
            this.DefaultPropertyValues = new Dictionary<string, string>();

            if (parent != null)
            {
                this.HasProperties = new HashSet<string>(this.HasProperties.Union(parent.HasProperties));
                this.DefaultPropertyValues = new Dictionary<string, string>(parent.DefaultPropertyValues);
            }

            this.HasProperties = new HashSet<string>(this.HasProperties.Union(
                from XElement e in element.Elements("HasProperty")
                select e.Value
                ));

            foreach (KeyValuePair<string, string> defaultPropertyValue in this.DefaultPropertyValues)
            {
                this.SetPropertyValue(defaultPropertyValue.Key, defaultPropertyValue.Value, null);
            }

            // 
            // Child flaws/violations.
            //

            foreach (XElement propertyCandidate in element.Elements())
            {
                if (SetPropertyValue(propertyCandidate.Name.ToString(), propertyCandidate.Value, propertyCandidate))
                {
                    this.DefaultPropertyValues[propertyCandidate.Name.ToString()] = propertyCandidate.Value;
                }
            }
        }

        public T FromXmlGeneric<T>(XElement e, T parent) where T : Profile
        {
            FromXml(e, parent);

            return this as T;
        }

        public virtual void Recalibrate(Target target)
        {
            if (this.RecalibrationEvent != null)
            {
                this.RecalibrationEvent.Invoke(target);
            }
        }

        [ThreadStatic]
        private static System.Security.Cryptography.SHA1CryptoServiceProvider Sha;

        public static string ComputeSHA1(params object[] values)
        {
            if (Sha == null)
            {
                Sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            }

            StringBuilder builder = new StringBuilder();

            foreach (object v in values)
            {
                builder.AppendFormat("{0},", v != null ? v.ToString() : "");
            }

            byte[] hash = Sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(builder.ToString()));

            return BitConverter.ToString(hash);
        }

        [Browsable(false)]
        public event RecalibrationDelegate RecalibrationEvent;

        public bool HasParent(Profile p)
        {
            return this == p || (this.Parent != null && this.Parent.HasParent(p));
        }

        [
         Category("General"),
         Description("A name for the profile."),
         XmlElement("Name")
        ]
        public string Name { get; set; }

        [
         Category("General"),
         Description("A symbolic name for the profile."),
         XmlElement("Symbol")
        ]
        public virtual string Symbol { get; set; }

        [Browsable(false), XmlIgnore]
        public string Alias { get; set; }

        [Browsable(false), XmlIgnore]
        public string Example { get; set; }

        [
         Category("General"),
         Description("A unique identifier for the profile."),
         XmlElement("Guid")
        ]
        public Guid Guid { get; set; }

        [
         Category("General"),
         Description("The type of model the profile belongs to.")
        ]
        public abstract ModelType ModelType { get; }

        /// <summary>
        /// The list of features that are related to this profile.
        /// </summary>
        [
         Browsable(false), 
         XmlArray("Features"),
         XmlArrayItem(typeof(Feature), ElementName="Feature")
        ]
        public List<Profile> Features { get; private set; }

        [Browsable(false), XmlIgnore]
        public virtual string Description
        {
            get
            {
                StringWriter writer = new StringWriter();

                writer.WriteLine("{0} profile: {1}", this.ModelType, this.FullSymbol);
                writer.WriteLine();

                foreach (ProfilePropertyInfo propertyInfo in this.Properties)
                {
                    writer.WriteLine("{0}={1}", propertyInfo.Name, propertyInfo.ValueString);
                }

                writer.WriteLine();

                return writer.ToString();
            }
        }

        [Browsable(false), XmlIgnore]
        public string ExampleOrParent
        {
            get
            {
                return (this.Example == null && this.Parent != null) ? this.Parent.ExampleOrParent : this.Example;
            }
        }

        [Browsable(false), XmlIgnore]
        public string FullName
        {
            get
            {
                return (this.Parent != null && this.Parent.Name != null) ? String.Format("{0}/{1}", this.Parent.FullName, this.Name) : this.Name;
            }
        }

        [
         Category("General"),
         Description("The full symbolic name for the profile."),
         XmlElement("FullSymbol"),
         ReadOnly(true)
        ]
        public string FullSymbol
        {
            get
            {
                if (this.fullSymbol != null)
                {
                    return this.fullSymbol;
                }
                else
                {
                    return (this.Parent != null && this.Parent.Symbol != null) ? String.Format("{0}/{1}", this.Parent.FullSymbol, this.Symbol) : this.Symbol;
                }
            }
            set
            {
                this.fullSymbol = value;
            }
        }
        private string fullSymbol;

        public override int GetHashCode()
        {
            return this.Guid.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is Profile && (obj as Profile).FullSymbol == this.FullSymbol;
        }

        /// <summary>
        /// The parent of this profile (a more general classification).
        /// </summary>
        [Browsable(false), XmlIgnore]
        public Profile Parent { get; set; }

        [Browsable(false), XmlIgnore]
        public abstract IEnumerable<Profile> Children { get; }

        [Browsable(false), XmlIgnore]
        public IEnumerable<Profile> PreOrderProfileComposition
        {
            get
            {
                if (this.Parent != null)
                {
                    foreach (Profile p in this.Parent.PreOrderProfileComposition)
                    {
                        yield return p;
                    }
                }

                yield return this;
            }
        }

        [Browsable(false), XmlIgnore]
        public IEnumerable<ProfilePropertyInfo> Properties
        {
            get
            {
                if (this.cachedProperties == null)
                {
                    cachedProperties =
                        (
                         from PropertyInfo info in this.GetType().GetProperties()
                         where info.GetCustomAttributes(typeof(ProfilePropertyAttribute), true).Length > 0
                         where info.PropertyType.FindInterfaces((type, obj) =>
                         {
                             return type == typeof(IProfilePropertyDictionary);
                         }, null).Length == 0
                         select new ProfilePropertyInfo(this, info)
                        ).Union(
                         from PropertyInfo info in this.GetType().GetProperties()
                         where info.GetCustomAttributes(typeof(ProfilePropertyAttribute), true).Length > 0
                         where info.PropertyType.FindInterfaces((type, obj) =>
                         {
                             return type == typeof(IProfilePropertyDictionary);
                         }, null).Length > 0
                         from KeyValuePair<string, string> pair in ((IProfilePropertyDictionary)info.GetGetMethod().Invoke(this, new object[] { })).GetKeyValueStrings()
                         select new ProfilePropertyInfo(this, info, String.Format("{0}.{1}", info.Name, pair.Key))
                        );
                }

                return this.cachedProperties;
            }
        }

        private IEnumerable<ProfilePropertyInfo> cachedProperties;

        public bool SetPropertyValue(string name, string value, XElement element)
        {
            ProfilePropertyInfo propertyInfo =
                (
                    from ProfilePropertyInfo info in this.Properties
                    where info.Name == name
                    select info
                ).FirstOrDefault();

            if (propertyInfo == null && element != null && element.Attribute("name") != null)
            {
                propertyInfo =
                    (
                        from ProfilePropertyInfo info in this.Properties
                        where info.Name.ToLower() == String.Format("{0}.{1}", name, element.Attribute("name").Value).ToLower()
                        select info
                    ).FirstOrDefault();
            }

            if (propertyInfo == null)
            {
                return false;
            }

            return SetPropertyValue(propertyInfo, value, element);
        }

        public bool SetPropertyValue(ProfilePropertyInfo property, string value, XElement element)
        {
            PropertyInfo match = property.PropertyInfo;

            if (property.IsPropertyDictionary && element != null)
            {
                IProfilePropertyDictionary dict = property.PropertyDictionary;

                dict.Set(element.Attribute("name").Value, value);

                return true;
            }

            if (this.HasProperties.Contains(match.Name) == false)
            {
                throw new NotSupportedException(String.Format("The profile {0} does not use property {1}.", this.Symbol, match.Name));
            }

            Type propertyType = match.PropertyType;

            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                propertyType = Nullable.GetUnderlyingType(propertyType);

                if (value == null || value.Length == 0)
                {
                    match.SetValue(this, null, null);
                    return true;
                }
            }

            if (propertyType == typeof(bool))
            {
                match.SetValue(this, Convert.ToBoolean(value), null);
            }
            else if (propertyType == typeof(uint))
            {
                match.SetValue(this, Convert.ToUInt32(value), null);
            }
            else if (propertyType.IsEnum)
            {
                match.SetValue(this, Enum.Parse(propertyType, value, true), null);
            }
            else
            {
                SetCustomProperty(new ProfilePropertyInfo(this, match), value);
            }

            return true;
        }

        public virtual void SetCustomProperty(ProfilePropertyInfo property, string value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// The set of dynamic properties this profile has.
        /// </summary>
        [XmlIgnore]
        internal HashSet<string> HasProperties { get; set; }

        /// <summary>
        /// Inherited parent property values.
        /// </summary>
        [XmlIgnore]
        internal Dictionary<string, string> DefaultPropertyValues { get; set; }

        /// <summary>
        /// True if this profile should be hidden from displayed versions of relevant graph(s). 
        /// </summary>
        /// <remarks>
        /// This helps hide extraneous details from a visible graph.
        /// </remarks>
        [Browsable(false), XmlIgnore]
        public bool Hidden { get; set; }
    }

    /// <summary>
    /// A reference to a profile (by guid and/or symbol).
    /// </summary>
    public class ProfileReference
    {
        /// <summary>
        /// The profile GUID being referenced.
        /// </summary>
        [XmlAttribute("guid")]
        public Guid Guid { get; set; }

        /// <summary>
        /// The symbol being referenced.
        /// </summary>
        [XmlAttribute("symbol")]
        public string Symbol { get; set; }
    }

    public class ProfilePropertyAttribute : Attribute
    {
        public ProfilePropertyAttribute()
            : this(null)
        {
        }

        public ProfilePropertyAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }
    }

    public class ProfilePropertyInfo
    {
        internal ProfilePropertyInfo(Profile profile, PropertyInfo propertyInfo, string dictionaryKey = null)
        {
            this.Profile = profile;
            this.PropertyInfo = propertyInfo;
            this.DictionaryKey = dictionaryKey;
        }

        public Profile Profile { get; private set; }
        public PropertyInfo PropertyInfo { get; private set; }
        public string DictionaryKey { get; private set; }

        private string InnerDictionaryKey
        {
            get
            {
                return this.DictionaryKey.Remove(0, this.PropertyInfo.Name.Length + 1);
            }
        }

        public void Set(string value)
        {
            if (this.IsPropertyDictionary)
            {
                //
                // Strip PropertyName/ from the start.
                //

                this.PropertyDictionary.Set(this.InnerDictionaryKey, value);
            }
            else
            {
                this.Profile.SetPropertyValue(this, value, null);
            }
        }

        public string Get()
        {
            object value;

            if (this.IsPropertyDictionary)
            {
                value = this.PropertyDictionary.GetKeyValueStrings().Where(x => x.Key == this.InnerDictionaryKey).Select(x => x.Value).FirstOrDefault(); ;
            }
            else
            {
                value = this.PropertyInfo.GetGetMethod().Invoke(this.Profile, new object[] { });
            }

            return (value == null) ? "" : value.ToString();
        }

        public string Name
        {
            get
            {
                if (this.IsPropertyDictionary)
                {
                    return this.DictionaryKey;
                }
                else
                {
                    return this.PropertyInfo.Name;
                }
            }
        }

        public string[] Values
        {
            get
            {

                Type propertyType;

                if (IsPropertyDictionary)
                {
                    propertyType = this.PropertyDictionary.GetValueType();
                }
                else
                {
                    propertyType = this.PropertyInfo.PropertyType;
                }

                if (Nullable.GetUnderlyingType(propertyType) != null)
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                if (propertyType.IsEnum)
                {
                    return Enum.GetNames(propertyType);
                }
                else if (propertyType == typeof(bool))
                {
                    return new string[] { "True", "False", "" };
                }
                else if (propertyType == typeof(uint))
                {
                    return new string[] { };
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public string ValueString
        {
            get
            {
                return Get();
            }
        }

        public override string ToString()
        {
            if (this.IsPropertyDictionary)
            {
                return this.DictionaryKey;
            }
            else
            {
                return this.PropertyInfo.Name;
            }
        }

        public bool IsPropertyDictionary
        {
            get
            {
                if (this.isPropertyDictionary == null)
                {
                    this.isPropertyDictionary = this.PropertyInfo.PropertyType.FindInterfaces((type, obj) =>
                    {
                        return type == typeof(IProfilePropertyDictionary);
                    }, null).Length > 0;
                }

                return this.isPropertyDictionary.Value;
            }
        }
        private bool? isPropertyDictionary;

        public IProfilePropertyDictionary PropertyDictionary
        {
            get
            {
                if (this.IsPropertyDictionary)
                {
                    return this.PropertyInfo.GetValue(this.Profile, null) as IProfilePropertyDictionary;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public interface IProfilePropertyDictionary
    {
        void Set(string keyName, string valueString);
        IEnumerable<KeyValuePair<string, string>> GetKeyValueStrings();

        Type GetValueType();
    }

    public class ProfilePropertyDictionary<K, V> : Dictionary<K, V>, IProfilePropertyDictionary 
    {
        public ProfilePropertyDictionary()
        {
        }

        public ProfilePropertyDictionary(ProfilePropertyDictionary<K, V> source)
            : base(source)
        {
        }

        public void Set(string keyName, string valueString)
        {
            this[ParseKey(keyName)] = ParseValue(valueString);
        }

        public IEnumerable<KeyValuePair<string, string>> GetKeyValueStrings()
        {
            foreach (KeyValuePair<K, V> pair in this.OrderBy(x => x.Key))
            {
                yield return new KeyValuePair<string, string>(pair.Key.ToString(), pair.Value == null ? "" : pair.Value.ToString());
            }
        }

        public Type GetValueType()
        {
            return typeof(V);
        }

        K ParseKey(string keyName)
        {
            if (typeof(K).IsEnum)
            {
                return (K)Enum.Parse(typeof(K), keyName, true);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        V ParseValue(string value)
        {
            if (value == null || value.Length == 0)
            {
                return default(V);
            }
            if (typeof(V) == typeof(bool?))
            {
                return (V)(object)(new bool?(Convert.ToBoolean(value)));
            }
            else if (typeof(V) == typeof(bool))
            {
                return (V)(object)Convert.ToBoolean(value);
            }
            else if (typeof(V) == typeof(uint))
            {
                return (V)(object)Convert.ToUInt32(value);
            }
            else if (typeof(V).IsEnum)
            {
                return (V)Enum.Parse(typeof(V), value, true);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
