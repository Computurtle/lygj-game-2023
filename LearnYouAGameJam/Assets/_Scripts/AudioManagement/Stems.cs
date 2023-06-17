using System;
using System.Diagnostics;

namespace LYGJ.AudioManagement {
    [Flags]
    public enum Stems {
        None        = 0,
        Melody      = 1 << 0,
        Instruments = 1 << 1,
        Bass        = 1 << 2,
        Drums       = 1 << 3,
        FullMix     = Melody | Instruments | Bass | Drums
    }
}
