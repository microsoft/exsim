// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UR.Graphing
{
	/// <summary>
	/// An invalid graph
	/// </summary>
	public class InvalidGraphException
		: Exception
	{
		public InvalidGraphException()
		{
		}

		public InvalidGraphException(string message)
			: base(message)
		{
		}
	}
}
