// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.ComponentModel;

namespace MSModel
{
    /// <summary>
    /// A model of classification features.
    /// </summary>
    public class FeatureModel : Model<Feature>
    {
        /// <summary>
        /// Defaut initializer.
        /// </summary>
        public FeatureModel()
        {
        }

        /// <summary>
        /// Initializes the model using feature profiles from the provided stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        public FeatureModel(Stream stream)
            : base(stream)
        {
        }

        protected override string[] ProfileElementNames
        {
            get
            {
                return new string[] {
                    "feature",
                    "property"
                };
            }
        }

        /// <summary>
        /// Creates an instance of an application from the supplied XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="parent">The parent profile.</param>
        /// <returns></returns>
        public static Feature CreateProfileInstanceSt(XElement element, Feature parent)
        {
            if (element.Name == "property")
            {
                Feature propertyFeature = new Feature() { IsPropertyFeature = true };

                propertyFeature.FromXml(element, parent);

                return propertyFeature;
            }
            else
            {
                return Profile.CreateInstance<Feature>(element, parent);
            }
        }

        /// <summary>
        /// Creates an instance of an application from the supplied XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="parent">The parent profile.</param>
        /// <returns></returns>
        protected override Feature CreateProfileInstance(XElement element, Feature parent)
        {
            return CreateProfileInstanceSt(element, parent);
        }

        /// <summary>
        /// The list of features in the model.
        /// </summary>
        public IEnumerable<Feature> Features
        {
            get
            {
                return this.Profiles;
            }
        }
    }

    /// <summary>
    /// A feature that is used during classification of a vulnerability.
    /// </summary>
    public class Feature : Profile
    {
        /// <summary>
        /// Default initializer.
        /// </summary>
        public Feature()
        {
        }

        /// <summary>
        /// Initializes this profile using the provided XML element.
        /// </summary>
        /// <param name="element">The XML element to deserialize.</param>
        /// <param name="parent">The parent profile (if any).</param>
        public override void FromXml(XElement element, Profile parent)
        {
            base.FromXml(element, parent);

            if (element.Element("help") != null)
            {
                this.Help = element.Element("help").Value;
            }

            this.ChildFeatures =
                new List<Feature>(
                     from XElement e in element.Elements()
                     where e.Name == "feature" || e.Name == "property"
                     select FeatureModel.CreateProfileInstanceSt(e, this)
                    );
        }

        /// <summary>
        /// Help text for this feature.
        /// </summary>
        [Browsable(false), XmlIgnore]
        public string Help { get; set; }

        [Browsable(false), XmlIgnore]
        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Feature; }
        }

        [Browsable(false), XmlIgnore]
        public bool IsPropertyFeature { get; set; }

        /// <summary>
        /// Child feature profiles.
        /// </summary>
        [Browsable(false), XmlIgnore]
        public override IEnumerable<Profile> Children
        {
            get { return this.ChildFeatures; }
        }

        /// <summary>
        /// Child feature profiles.
        /// </summary>
        [Browsable(false), XmlIgnore]
        public IEnumerable<Feature> ChildFeatures { get; set; }
    }
}
