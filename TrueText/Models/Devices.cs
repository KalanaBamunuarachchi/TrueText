﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TrueText.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string Status { get; set; } = string.Empty; 
        
    }
}
