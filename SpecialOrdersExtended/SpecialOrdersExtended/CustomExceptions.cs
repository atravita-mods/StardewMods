using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecialOrdersExtended
{
    public class UnexpectedEnumValueException<T> : Exception
    {
        public UnexpectedEnumValueException(T value)
            : base($"Enum {typeof(T).Name} recieved unexpected value {value}")
        {
        }
    }
}
