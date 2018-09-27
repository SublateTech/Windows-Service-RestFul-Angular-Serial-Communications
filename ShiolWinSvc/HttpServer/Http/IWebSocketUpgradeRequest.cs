using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

namespace m.Http
{
    public interface IWebSocketUpgradeRequest
    {
        Uri Url { get; }
        string Path { get; }
        IReadOnlyDictionary<string, string> PathVariables { get; }
        NameValueCollection Query { get; }

        WebSocketUpgradeResponse.AcceptUpgradeResponse AcceptUpgrade(Action<IWebSocketSession> onAccepted);

        WebSocketUpgradeResponse.RejectUpgradeResponse RejectUpgrade(HttpStatusCode reason);
    }
}
