// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text;

namespace UR.Ui
{
	/// <summary>
	/// Callback that allows command line switches to interpret/handle their invocation
	/// </summary>
	/// <param name="context">An opaque context</param>
	/// <param name="value">The value passed to the switch, if any</param>
	/// <returns>True if the switch is handled successfully</returns>
	public delegate void CommandLineSwitchHandler(object context, string value);

	/// <summary>
	/// A command line switch
	/// </summary>
	public class CommandLineSwitch
	{
		/// <summary>
		/// Initializes the command line switch
		/// </summary>
		/// <param name="fullPrefix">The command line switch prefix</param>
		/// <param name="handler">The callback handler</param>
		public CommandLineSwitch(string fullPrefix, CommandLineSwitchHandler handler)
		{
			this.fullPrefix = fullPrefix;
			this.shortPrefix = null;
			this.handler = handler;
			this.hasValue = false;
		}

		/// <summary>
		/// Initializes the command line switch
		/// </summary>
		/// <param name="fullPrefix">The command line switch prefix</param>
		/// <param name="hasValue">True if this switch requires a parameter value</param>
		/// <param name="handler">The callback handler</param>
		public CommandLineSwitch(string fullPrefix, bool hasValue, CommandLineSwitchHandler handler)
			: this(fullPrefix, handler)
		{
			this.hasValue = hasValue;
		}

		/// <summary>
		/// Initializes the command line switch
		/// </summary>
		/// <param name="fullPrefix">The command line switch prefix</param>
		/// <param name="shortPrefix">The short prefix for this switch</param>
		/// <param name="handler">The callback handler</param>
		public CommandLineSwitch(string fullPrefix, string shortPrefix, CommandLineSwitchHandler handler)
			: this(fullPrefix, handler)
		{
			this.shortPrefix = shortPrefix;
		}

		/// <summary>
		/// Initializes the command line switch
		/// </summary>
		/// <param name="fullPrefix">The command line switch prefix</param>
		/// <param name="shortPrefix">The short prefix for this switch</param>
		/// <param name="hasValue">True if this switch requires a parameter value</param>
		/// <param name="handler">The callback handler</param>
		public CommandLineSwitch(string fullPrefix, string shortPrefix, bool hasValue, CommandLineSwitchHandler handler)
			: this(fullPrefix, shortPrefix, handler)
		{
			this.hasValue = hasValue;
		}

		/// <summary>
		/// Checks to see if the supplied switch is equal to this switch
		/// </summary>
		/// <param name="sw">The switch string</param>
		/// <returns>True if it's a match</returns>
		public bool IsSwitch(string sw)
		{// || (shortPrefix != null && sw.StartsWith(shortPrefix));
			return sw == fullPrefix ||
				// Or, allow the caller to use other switches, such as -
				((fullPrefix.StartsWith("/") || fullPrefix.StartsWith("-")) &&
				 ((sw.Remove(0, 1) == fullPrefix.Remove(0, 1)) || ((shortPrefix != null) && (sw.Remove(0, 1) == shortPrefix.Remove(0, 1)))));
		}

		/// <summary>
		/// The full prefix of this switch, such as /help
		/// </summary>
		public string FullPrefix
		{
			get { return fullPrefix; }
		}
		private string fullPrefix;

		/// <summary>
		/// The short prefix of this switch, such as /h
		/// </summary>
		public string ShortPrefix
		{
			get { return shortPrefix; }
		}
		private string shortPrefix;

		/// <summary>
		/// True if the switch is required to have a parameter value
		/// </summary>
		public bool HasValue
		{
			get { return hasValue; }
		}
		private bool hasValue;

		/// <summary>
		/// The callback to invoke when the switch is used
		/// </summary>
		public CommandLineSwitchHandler Handler
		{
			get { return handler; }
		}
		private CommandLineSwitchHandler handler;
	}

	/// <summary>
	/// Parses an array of command line arguments
	/// </summary>
	public class CommandLineParser
	{
		/// <summary>
		/// Initializes the parser
		/// </summary>
		/// <param name="switches">The set of switches to apply</param>
		public CommandLineParser(CommandLineSwitch[] switches)
		{
			this.switches = switches;
		}

		/// <summary>
		/// Initializes the parser
		/// </summary>
		/// <param name="switches">The set of switches to apply</param>
		/// <param name="args">The arguments to parse</param>
		public CommandLineParser(CommandLineSwitch[] switches, string[] args)
			: this(switches)
		{
			Parse(args);
		}

		/// <summary>
		/// Parses the supplied arguments
		/// </summary>
		/// <param name="args">The argument array to parse</param>
		public void Parse(string[] args)
		{
			Parse(new List<string>(args));
		}

		/// <summary>
		/// Parses the supplied arguments
		/// </summary>
		/// <param name="args">The argument array to parse</param>
		public void Parse(List<string> mutableArgs)
		{
			Parse(mutableArgs, null);
		}

		/// <summary>
		/// Parses the supplied argument array
		/// </summary>
		/// <param name="args">The argument array to parse</param>
		/// <param name="context">The opaque context to pass to switch handlers</param>
		public void Parse(List<string> argv, object context)
		{
			int argi = 0;

			while (argi < argv.Count)
			{
				string name = argv[argi];
				string value = null;
				bool found = false;

				foreach (CommandLineSwitch sw in switches)
				{
					if (!sw.IsSwitch(name))
						continue;

					found = true;
					argv.RemoveAt(argi);

					if (sw.HasValue)
					{
						if (argi >= argv.Count)
							throw new ArgumentOutOfRangeException(
								String.Format("{0} requires an argument", sw.FullPrefix));

						value = argv[argi];
						argv.RemoveAt(argi);
					}

					sw.Handler(context, value);
				}

				if (!found)
					argi++;
			}
		}

		/// <summary>
		/// The array of command line switches
		/// </summary>
		private CommandLineSwitch[] switches;
	}

	/// <summary>
	/// Exception that can be raised by command line switch handlers
	/// </summary>
	public class CommandLineSwitchException : Exception
	{
		public CommandLineSwitchException(string prefix)
			: base(String.Format("The {0} command is not in a proper format", prefix))
		{
		}
	}
}
