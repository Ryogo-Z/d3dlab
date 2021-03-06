﻿using D3DLab.Core.Render;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace D3DLab.Core.Test {
    public interface IComponent : IDisposable {
        Guid Guid { get; }
    }
    
    public abstract class Component : IComponent {
        public Guid Guid { get; }

        protected Component() {
            Guid = Guid.NewGuid();
        }

        public void Dispose() {

        }
    }
}
