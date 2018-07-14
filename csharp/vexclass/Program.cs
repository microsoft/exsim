// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vexclass
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            (new ClassificationForm()).ShowDialog();
        }
    }
}
