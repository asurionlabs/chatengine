/*
AVA Chat Engine is a chat bot API.

Copyright (C) 2015-2019  Asurion, LLC

AVA Chat Engine is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

AVA Chat Engine is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with AVA Chat Engine.  If not, see <https://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Linq;
using ChatEngine.Models;
using System.Collections.Concurrent;

namespace ChatEngine.Hubs
{
    public class NodeJSHub : Hub
    {
        static readonly Random random = new Random();
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly Dictionary<string, List<Connection>> UserMap = new Dictionary<string, List<Connection>>();
        static readonly NodeErrorResponse[] DisconnectErrorResponse = new NodeErrorResponse[] { new NodeErrorResponse { Stack = "Node engine disconnected" } };

        public void Connect(string user)
        {
            lock (UserMap)
            {
                if (!UserMap.ContainsKey(user))
                    UserMap[user] = new List<Connection>();

                UserMap[user].Add(new Connection(Context.ConnectionId));
            }
        }

        public static Connection GetConnection(string userId)
        {
            if (!NodeJSHub.UserMap.ContainsKey(userId))
                throw new ApplicationException("Invalid userId for NodeJS execution");

            return UserMap[userId].OrderBy(l => random.Next()).First();
        }

        public void SetResult(string resultId, object value = null)
        {
            var subject = (from user in UserMap
                                  from connection in user.Value
                                  where connection.ConnectionId == Context.ConnectionId
                                  from result in connection.Results
                                  where result.Key == resultId
                                  select result.Value).FirstOrDefault();

            if (subject != null)
                subject.OnNext(value);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            lock (UserMap)
            {
                var userConnection = (from user in UserMap
                            from connection in user.Value
                            where connection.ConnectionId == Context.ConnectionId
                            select new { userId = user.Key, connection }).FirstOrDefault();

                logger.DebugFormat("Node Client disconnected. {0}", userConnection.userId);

                // Trigger all pending results on this connection
                foreach (var result in userConnection.connection.Results.Values.ToList())
                {
                    result.OnNext(DisconnectErrorResponse);
                }

                UserMap[userConnection.userId].Remove(userConnection.connection);
                if (UserMap[userConnection.userId].Count == 0)
                    UserMap.Remove(userConnection.userId);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }

    public class Connection
    {
        public Connection(string connectionId)
        {
            ConnectionId = connectionId;
            Results = new ConcurrentDictionary<string, ISubject<object>>();
        }

        public string ConnectionId { get; private set; }

        public ConcurrentDictionary<string, ISubject<object>> Results { get; private set; }
    }
}
