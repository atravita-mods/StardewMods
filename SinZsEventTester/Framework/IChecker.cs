using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinZsEventTester.Framework;
internal interface IChecker: IDisposable
{
    bool IsDisposed { get; }
}
