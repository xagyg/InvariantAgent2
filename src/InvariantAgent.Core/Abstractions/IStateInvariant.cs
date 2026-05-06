using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions
{
    public interface IStateInvariant : IInvariant<StateProjection>
    {
    }
}
