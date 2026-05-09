using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions;

public interface IEventStore
{
    void Append(AgentEvent evt);

    IList<AgentEvent> LoadAll();

  //  IList<AgentEvent> LoadSession(string sessionId);

  //  void Clear();
}