using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Model
{
    public interface IInvariant<T>
    {
        string Name { get; }

        InvariantResult Evaluate(T input);
    }
}
