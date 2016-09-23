﻿using BatchGuy.App.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchGuy.App.FFMSIndex.Interfaces
{
    public interface IFFMSIndexBatchFileWriteService
    {
        ErrorCollection Errors { get; }
        ErrorCollection Write();
        bool IsValid();
        void Delete();
    }
}