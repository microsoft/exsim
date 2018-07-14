// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;

namespace MSModel
{
    /// <summary>
    /// A hardware model.
    /// </summary>
    public class HardwareModel : Model<Hardware>
    {
        /// <summary>
        /// The list of hardware profiles in this model.
        /// </summary>
        public IEnumerable<Hardware> HardwareList
        {
            get { return this.Profiles; } 
        }
    }

    /// <summary>
    /// A hardware profile.
    /// </summary>
    /// <remarks>
    /// This class specifies the supported security features of the process and other related hardware components.
    /// </remarks>
    public class Hardware : Profile
    {
        /// <summary>
        /// Default initializer.
        /// </summary>
        public Hardware()
        {
        }

        /// <summary>
        /// Initializes a hardware profile from an XML element.
        /// </summary>
        /// <param name="element">The XML element describing this profile.</param>
        /// <param name="parent">The parent hardware profile (if any)</param>
        public Hardware(XElement element, Profile parent)
            : base(element, parent)
        {
        }

        /// <summary>
        /// Deserializes a hardware profile from XML.
        /// </summary>
        /// <param name="element">The XML element to deserialize from.</param>
        /// <param name="parent">The parent hardware profile (if any).</param>
        public override void FromXml(XElement element, Profile parent)
        {
            base.FromXml(element, parent);

            this.ChildHardware =
                new List<Hardware>(
                    from XElement e in element.Elements("Hardware")
                    select new Hardware(e, this)
                    );
        }

        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Hardware; }
        }

        /// <summary>
        /// Child hardware profiles.
        /// </summary>
        public override IEnumerable<Profile> Children
        {
            get { return this.ChildHardware; }
        }

        /// <summary>
        /// Child hardware profiles.
        /// </summary>
        public IEnumerable<Hardware> ChildHardware { get; set; }

        /// <summary>
        /// The number of virtual address bits supported by the processor.
        /// </summary>
        [ProfileProperty]
        public uint? AddressBits { get; set; }

        /// <summary>
        /// The processor architecture family (x86, arm, etc).
        /// </summary>
        [ProfileProperty]
        public ArchitectureFamily? ArchitectureFamily { get; set; }

        /// <summary>
        /// The policy for non-executable page support.
        /// </summary>
        [ProfileProperty]
        public MitigationPolicy? NXPolicy { get; set; }

        /// <summary>
        /// The policy for supervisor mode execution prevention support.
        /// </summary>
        [ProfileProperty]
        public MitigationPolicy? SMEPPolicy { get; set; }
    }
}
