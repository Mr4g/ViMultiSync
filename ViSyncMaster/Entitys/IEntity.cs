﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Entitys
{
    public interface IEntity
    {
        long Id { get; set; }
        string SendStatus { get; set; } 
    }
}
