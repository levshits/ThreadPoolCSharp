﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreadPoolLibrary
{
    public interface ITask
    {
        void Execute();
    }
}
