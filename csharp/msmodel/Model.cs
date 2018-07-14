// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using UR.Graphing;

namespace MSModel
{
    public class MemorySafetyModel
    {
        public class Configuration
        {
            public string VulnerabilityFeatureModelXmlPath { get; set; }
            public string VectorFeatureModelXmlPath { get; set; }
            public string FlawModelXmlPath { get; set; }
            public string ViolationModelXmlPath { get; set; }
            public string HardwareModelXmlPath { get; set; }
            public string OperatingSystemModelXmlPath { get; set; }
            public string ApplicationModelXmlPath { get; set; }
        }

        public MemorySafetyModel()
            : this(new Configuration())
        {
        }

        public MemorySafetyModel(Configuration configuration)
        {
            this.Config = configuration;

            Build();
        }

        /// <summary>
        /// Constructs the memory safety model.
        /// </summary>
        private void Build()
        {
            //
            // Build child models.
            //

            this.VulnerabilityFeatureModel = BuildModel<FeatureModel>(this.Config.VulnerabilityFeatureModelXmlPath, @"MSModel.Profiles.Features.Vulnerability.xml");
            this.VectorFeatureModel = BuildModel<FeatureModel>(this.Config.VectorFeatureModelXmlPath, @"MSModel.Profiles.Features.Vector.xml");


            this.FlawModel = BuildModel<FlawModel>(this.Config.FlawModelXmlPath, @"MSModel.Profiles.Flaws.xml");
            this.ViolationModel = BuildModel<ViolationModel>(this.Config.ViolationModelXmlPath);
            this.HardwareModel = BuildModel<HardwareModel>(this.Config.HardwareModelXmlPath, @"MSModel.Profiles.Hardware.xml");
            this.OperatingSystemModel = BuildModel<OperatingSystemModel>(this.Config.OperatingSystemModelXmlPath, @"MSModel.Profiles.OperatingSystem.xml");
            this.ApplicationModel = BuildModel<ApplicationModel>(this.Config.ApplicationModelXmlPath, @"MSModel.Profiles.Application.xml");           
        }

        private ModelType BuildModel<ModelType>(string xmlPath, string resourceName = null) where ModelType : IModel, new()
        {
            Stream stream;

            if (xmlPath != null)
            {
                stream = File.OpenRead(xmlPath);
            }
            else if (resourceName != null)
            {
                stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
            }
            else
            {
                return new ModelType();
            }

            using (stream)
            {
                ModelType model = new ModelType();

                model.FromStream(stream);

                return model;
            }
        }

        /// <summary>
        /// Generates a DOT file describing the relationships between flaws and violations.
        /// </summary>
        /// <remarks>
        /// Blue edges are "composition" edges (e.g. x->y means y is an instance of x).
        /// Red edges are "transitive" edges (e.g. x->y means x leads to y).
        /// Green edges are "transitive" edges from a flaw to a violation.
        /// Purple edges are "transitive" edges from a violation to a flaw.
        /// </remarks>
        /// <param name="path">The output file path.</param>
        public void SaveGraph(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("digraph flaws {");
                writer.WriteLine("rankdir=LR;");
                writer.WriteLine("compound=true;");
                writer.WriteLine("model=subset;");

                writer.WriteLine("subgraph cluster_flaws {");
                writer.WriteLine("label=\"memory safety flaws\";");

                foreach (Flaw flaw in this.FlawModel.Profiles)
                {
                    if (flaw.Hidden)
                    {
                        continue;
                    }

                    writer.WriteLine("{0}", flaw);
                }

                foreach (Edge edge in this.FlawModel.CompositionMap.Edges)
                {
                    Profile source = edge.Source as Profile;
                    Profile target = edge.Target as Profile;

                    if (source.Hidden || target.Hidden)
                    {
                        continue;
                    }

                    writer.WriteLine("{0} -> {1} [color=blue]", edge.Source, edge.Target);
                }

                foreach (Edge edge in this.FlawModel.TransitiveMap.Edges)
                {
                    Profile source = edge.Source as Profile;
                    Profile target = edge.Target as Profile;

                    if (source.Hidden || target.Hidden)
                    {
                        continue;
                    }
                   
                    writer.WriteLine("{0} -> {1} [color=red]", edge.Source, edge.Target);
                }

                writer.WriteLine("}");

                writer.WriteLine("subgraph cluster_violations {");
                writer.WriteLine("label=\"memory safety violations\";");

                foreach (Violation violation in this.ViolationModel.Profiles)
                {
                    writer.WriteLine("{0}", violation);
                }

                foreach (Edge edge in this.ViolationModel.CompositionMap.Edges)
                {
                    writer.WriteLine("{0} -> {1} [color=blue]", edge.Source, edge.Target);
                }

                writer.WriteLine("}");

                foreach (Edge e in this.TransitiveMap.Edges)
                {
                    Profile source = e.Source as Profile;
                    Profile target = e.Target as Profile;

                    if (source.Hidden || target.Hidden)
                    {
                        continue;
                    }

                    if ((source is Flaw) && (target is Violation))
                    {
                        writer.WriteLine("{0} -> {1} [color=green]", source, target);
                    }
                    else if ((source is Violation) && (target is Flaw))
                    {
                        writer.WriteLine("{0} -> {1} [color=purple]", source, target);
                    }
                }

                writer.WriteLine("}");
            }
        }

        public IEnumerable<Violation> GetTransitiveViolations(Flaw flaw)
        {
            ForwardGraphNavigator nav = new ForwardGraphNavigator(this.TransitiveMap);

            HashSet<Violation> violations = new HashSet<Violation>();

            nav.Navigate(
                flaw,
                new DelegateGraphVisitor(visit: (graph, vertex) =>
                {

                    if (vertex is Violation)
                    {
                        violations.Add((vertex as Violation).Clone() as Violation);
                        throw new SkipChildrenException();
                    }

                    return true;
                }));

            return violations;
        }

        /// <summary>
        /// Memory safety model configuration.
        /// </summary>
        public Configuration Config { get; private set; }

        /// <summary>
        /// High level model of features that describe a vulnerability.
        /// </summary>
        public FeatureModel VulnerabilityFeatureModel { get; private set; }

        /// <summary>
        /// High level model of features that describe an exploit.
        /// </summary>
        public FeatureModel ExploitFeatureModel { get; private set; }

        /// <summary>
        /// High level model of features that describe an attack vector.
        /// </summary>
        public FeatureModel VectorFeatureModel { get; private set; }

        /// <summary>
        /// Model of memory safety flaws.
        /// </summary>
        public FlawModel FlawModel { get; private set; }

        /// <summary>
        /// Model of memory safety violations.
        /// </summary>
        public ViolationModel ViolationModel { get; private set; }

        /// <summary>
        /// Model of hardware configurations.
        /// </summary>
        public HardwareModel HardwareModel { get; private set; }

        /// <summary>
        /// Model of operating system configurations.
        /// </summary>
        public OperatingSystemModel OperatingSystemModel { get; private set; }

        /// <summary>
        /// Model of application configurations.
        /// </summary>
        public ApplicationModel ApplicationModel { get; private set; }

        /// <summary>
        /// Maps a flaw to the set of violations that can be reached from it.
        /// </summary>
        public Dictionary<Flaw, IEnumerable<Violation>> FlawTransitiveViolations { get; private set; }

        /// <summary>
        /// Maps memory safety flaws and violations to one another.
        /// </summary>
        public Graph TransitiveMap { get; private set; }

        /// <summary>
        /// A map of the way that flaws and violations are composed.
        /// </summary>
        public Graph CompositionMap { get; private set; }
    }

    /// <summary>
    /// Model interface.
    /// </summary>
    public interface IModel
    {
        void FromStream(Stream stream);

        ModelType ModelType { get; }
        IEnumerable<Profile> ProfilesRaw { get; }
        Graph CompositionMap { get; }
    }

    public enum ModelType
    {
        Unknown,
        Hardware,
        OperatingSystem,
        Application,
        Flaw,
        Violation,
        Technique,
        Feature,
        Vulnerability,
        Exploit,
        ExploitTarget
    }

    /// <summary>
    /// An abstract model.
    /// </summary>
    /// <typeparam name="T">The profile type that is being modeled.</typeparam>
    public abstract class Model<T> : IModel where T : Profile, new()
    {
        public Model()
        {
        }

        /// <summary>
        /// Initializes a model with profiles described by the specified XML file.
        /// </summary>
        /// <param name="stream"></param>
        public Model(Stream stream)
        {
            FromStream(stream);
        }

        public void FromStream(Stream stream)
        {
            this.Profiles = new List<T>();
            this.SymbolMap = new Dictionary<string, T>();
            this.AliasMap = new Dictionary<string, T>();
            this.GuidMap = new Dictionary<Guid, T>();

            XElement root = XElement.Load(stream);
            T rootProfile = new T().FromXmlGeneric<T>(root, null);

            var baseProfiles =
                from XElement e in root.Elements()
                from string expectedName in this.ProfileElementNames
                where e.Name == expectedName
                select CreateProfileInstance(e, rootProfile);

            //
            // Map symbols to profile instances.
            //

            Queue<T> profileQueue = new Queue<T>(baseProfiles);

            while (profileQueue.Count > 0)
            {
                T profile = profileQueue.Dequeue();

                this.Profiles.Add(profile);

                this.SymbolMap[profile.Symbol] = profile;
                this.GuidMap[profile.Guid] = profile;

                if (profile.Alias != null)
                {
                    this.AliasMap[profile.Alias] = profile;
                }

                foreach (Profile child in profile.Children)
                {
                    profileQueue.Enqueue(child as T);
                }
            }

            //
            // Build composition and transitive maps.
            //

            this.BuildCompositionMap();
            this.BuildTransitiveMap();

            //
            // Build the fully qualified symbol map using the composition map that was generated.
            //

            this.BuildFullyQualifiedMaps();
        }

        protected virtual string[] ProfileElementNames
        {
            get
            {
                return new string[] { typeof(T).Name };
            }
        }

        /// <summary>
        /// Creates an instance of a profile given an XML element and a parent profile.
        /// </summary>
        /// <param name="element">The XML element for this profile.</param>
        /// <param name="parent">The profile's parent.</param>
        /// <returns>A profile instance.</returns>
        protected virtual T CreateProfileInstance(XElement element, T parent)
        {
            return (new T().FromXmlGeneric<T>(element, parent));
        }

        /// <summary>
        /// Builds the composition map for the model based on profile composition hierarchy.
        /// </summary>
        protected virtual void BuildCompositionMap()
        {
            this.CompositionMap = new DirectedGraph();

            foreach (Profile p in this.Profiles)
            {
                if (p.Parent != null && p.Parent.Symbol != null)
                {
                    this.CompositionMap.AddEdge(p.Parent, p);
                }
                else
                {
                    this.CompositionMap.AddVertex(p);
                }
            }
        }

        /// <summary>
        /// Fully qualified symbol work item.
        /// </summary>
        internal class FQSMapWorkItem
        {
            public FQSMapWorkItem(T profile, FQSMapWorkItem parent)
            {
                this.Profile = profile;

                if (parent != null)
                {
                    this.Visited = new HashSet<T>(parent.Visited);
                    this.ProfileSequence = new List<T>(parent.ProfileSequence);
                }
                else
                {
                    this.Visited = new HashSet<T>();
                    this.ProfileSequence = new List<T>();
                }

                this.Visited.Add(profile);
                this.ProfileSequence.Add(profile);
            }

            public HashSet<T> Visited { get; set; }
            public T Profile { get; set; }
            public List<T> ProfileSequence { get; set; }
        }

        /// <summary>
        /// Builds dictionaries relating fully qualified symbol names to profiles.
        /// </summary>
        protected void BuildFullyQualifiedMaps()
        {
            Queue<FQSMapWorkItem> workItemQueue = new Queue<FQSMapWorkItem>(
                from T p in this.CompositionMap.Roots
                select new FQSMapWorkItem(p, null)
                );

            this.FullyQualifiedNameMap = new Dictionary<string, T>();
            this.FullyQualifiedSymbolMap = new Dictionary<string, T>();

            while (workItemQueue.Count > 0)
            {
                FQSMapWorkItem wi = workItemQueue.Dequeue();

                if (wi.Profile.FullSymbol != null)
                {
                    this.FullyQualifiedSymbolMap[wi.Profile.FullSymbol] = wi.Profile;
                }

                if (wi.Profile.FullName != null)
                {
                    this.FullyQualifiedNameMap[wi.Profile.FullName] = wi.Profile;
                }

                foreach (DirectedEdge edge in this.CompositionMap.Successors(wi.Profile))
                {
                    T target = (T)edge.Target;

                    if (wi.Visited.Contains(target))
                    {
                        continue;
                    }

                    workItemQueue.Enqueue(new FQSMapWorkItem(target, wi));
                }
            }
        }

        internal struct SaveAsFeatureXmlWorkItem
        {
            public XElement ParentElement { get; set; }
            public T Profile { get; set; }
        }

        /// <param name="path"></param>
        public void SaveAsFeatureXml(string @path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                Queue<SaveAsFeatureXmlWorkItem> workItems = new Queue<SaveAsFeatureXmlWorkItem>();
                XElement rootElement = new XElement("features");

                foreach (T root in this.CompositionMap.Roots)
                {
                    workItems.Enqueue(new SaveAsFeatureXmlWorkItem()
                    {
                        ParentElement = rootElement,
                        Profile = root
                    });
                }

                while (workItems.Count > 0)
                {
                    SaveAsFeatureXmlWorkItem wi = workItems.Dequeue();

                    XElement element = new XElement("feature");
                    element.Add(new XAttribute("name", wi.Profile.Name));
                    element.Add(new XAttribute("keyword", wi.Profile.Symbol));

                    wi.ParentElement.Add(element);

                    foreach (DirectedEdge edge in this.CompositionMap.Successors(wi.Profile))
                    {
                        T target = (T)edge.Target;

                        workItems.Enqueue(new SaveAsFeatureXmlWorkItem()
                        {
                            ParentElement = element,
                            Profile = target
                        });
                    }
                }

                rootElement.Save(writer);
            }
        }

        /// <summary>
        /// Resolves a profile using its symbol or alias.
        /// </summary>
        /// <param name="symbolOrAlias">The symbol or alias of the profile.</param>
        /// <returns>The profile instance.</returns>
        public T GetProfile(string symbolOrAlias)
        {
            try
            {
                return this.SymbolMap[symbolOrAlias];
            }
            catch (KeyNotFoundException)
            {
                return this.AliasMap[symbolOrAlias];
            }
        }

        /// <summary>
        /// Builds the transitive map for the model.
        /// </summary>
        protected virtual void BuildTransitiveMap()
        {
        }

        /// <summary>
        /// Gets the profile and its parent profiles for the supplied profile GUID.
        /// </summary>
        /// <param name="guid">The GUID to search for.</param>
        /// <returns>An enumeration of the matching profile and its parents.</returns>
        public IEnumerable<T> GetProfileAndParentsByGuid(Guid guid) 
        {
            Profile p = this.Profiles.Where(x => x.Guid == guid).FirstOrDefault();

            while (p != null)
            {
                yield return (T)p;

                p = p.Parent;
            }
        }

        /// <summary>
        /// The list of profiles (flaws, violations, etc) in the model.
        /// </summary>
        public List<T> Profiles { get; protected set; }

        /// <summary>
        /// The list of profiles in the model.
        /// </summary>
        public IEnumerable<Profile> ProfilesRaw
        {
            get
            {
                return this.Profiles;
            }
        }

        /// <summary>
        /// The type of profile contained in this model.
        /// </summary>
        public ModelType ModelType
        {
            get
            {
                if (ModelTypeMap == null)
                {
                    ModelTypeMap = new Dictionary<Type, ModelType>();

                    ModelTypeMap[typeof(Application)] = MSModel.ModelType.Application;
                    ModelTypeMap[typeof(Hardware)] = MSModel.ModelType.Hardware;
                    ModelTypeMap[typeof(OperatingSystem)] = MSModel.ModelType.OperatingSystem;
                    ModelTypeMap[typeof(Flaw)] = MSModel.ModelType.Flaw;
                    ModelTypeMap[typeof(Violation)] = MSModel.ModelType.Violation;
                    ModelTypeMap[typeof(Feature)] = MSModel.ModelType.Feature;
                }

                try
                {
                    return ModelTypeMap[typeof(T)];
                }
                catch (KeyNotFoundException)
                {
                    return MSModel.ModelType.Unknown;
                }
            }
        }

        private static Dictionary<Type, ModelType> ModelTypeMap;

        /// <summary>
        /// A map from symbol name to profile.
        /// </summary>
        public Dictionary<string, T> SymbolMap { get; private set; }

        /// <summary>
        /// A map from alias to profile.
        /// </summary>
        public Dictionary<string, T> AliasMap { get; private set; }

        /// <summary>
        /// A map from a fully qualified symbol name to a profile.
        /// </summary>
        public Dictionary<string, T> FullyQualifiedSymbolMap { get; private set; }

        /// <summary>
        /// A map from a fully qualified name to a profile.
        /// </summary>
        public Dictionary<string, T> FullyQualifiedNameMap { get; private set; }

        /// <summary>
        /// A map from a profile GUID to a profile.
        /// </summary>
        public Dictionary<Guid, T> GuidMap { get; private set; }

        /// <summary>
        /// A map describing the transitive relation between profiles (X can lead to Y).
        /// </summary>
        public Graph TransitiveMap { get; protected set; }

        /// <summary>
        /// A map describing the "instance of" relation (X is an instance of Y).
        /// </summary>
        public Graph CompositionMap { get; protected set; }
    }
}
