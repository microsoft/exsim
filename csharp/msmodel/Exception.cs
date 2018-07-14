// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSModel
{
    public class ConstraintNotSatisfied : Exception
    {
        public ConstraintNotSatisfied(string message)
            : base(message)
        {
        }
    }
}
