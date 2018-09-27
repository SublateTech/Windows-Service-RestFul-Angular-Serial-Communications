﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using m.Http.Backend;
using m.Http.Metrics;
using m.Http.Routing;

using m.Utils;
using ShiolWinSvc;

namespace m.Http
{
    public class Router : LifeCycleBase, IEnumerable<RouteTable>
    {
        public struct HandleResult
        {
            public readonly int MatchedRouteTableIndex;
            public readonly int MatchedEndpointIndex;
            public readonly HttpResponse HttpResponse;

            public HandleResult(int matchedRouteTableIndex, int matchedEndpointIndex, HttpResponse httpResponse)
            {
                MatchedRouteTableIndex = matchedRouteTableIndex;
                MatchedEndpointIndex = matchedEndpointIndex;
                HttpResponse = httpResponse;
            }
        }

        readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(Router));

        static readonly HttpResponse NotFound = new ErrorResponse(HttpStatusCode.NotFound);
        static readonly HttpResponse BadRequest = new ErrorResponse(HttpStatusCode.BadRequest);

        readonly RouteTable[] routeTables;
        readonly RequestLogs requestLogs;
        readonly WaitableTimer timer;

        readonly RateLimitedEndpoint[] rateLimitedEndpoints;

        internal RouterMetrics Metrics { get; private set; }
        public RouteTable this[int RouteTableIndex] { get { return routeTables[RouteTableIndex]; } }
        public int Length { get { return routeTables.Length; } }

        public Router(RouteTable routeTable, int requestLogsSize=8192, int timerPeriodMs=100) : this(new [] { routeTable }, requestLogsSize, timerPeriodMs) { }

        Router(RouteTable[] routeTables, int requestLogsSize, int timerPeriodMs)
        {
            this.routeTables = routeTables;

            rateLimitedEndpoints = routeTables.SelectMany(table => table.Where(ep => ep is RateLimitedEndpoint).Cast<RateLimitedEndpoint>())
                                              .ToArray();

            requestLogs = new RequestLogs(this, requestLogsSize);
            Metrics = new RouterMetrics(this);
            timer = new WaitableTimer("RouterTimer",
                                      TimeSpan.FromMilliseconds(timerPeriodMs),
                                      new [] {
                                          new WaitableTimer.Job("UpdateRateLimitBuckets", UpdateRateLimitBuckets),
                                          new WaitableTimer.Job("ProcessRequestLogs", ProcessRequestLogs)
                                      });
        }

        public IEnumerator<RouteTable> GetEnumerator()
        {
            return ((IEnumerable<RouteTable>)routeTables).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected override void OnStart()
        {
            timer.Start();
        }

        protected override void OnShutdown()
        {
            timer.Shutdown();
        }

        void UpdateRateLimitBuckets()
        {
            for (int i=0; i<rateLimitedEndpoints.Length; i++)
            {
                rateLimitedEndpoints[i].UpdateRateLimitBucket();
            }
        }

        void ProcessRequestLogs()
        {
            IEnumerable<RequestLogs.Log>[][] logs;
            int drained = requestLogs.Drain(out logs);
            if (drained > 0)
            {
                Metrics.Update(logs);
            }
        }

        int MatchRouteTable(string host)
        {
            for (int i=0; i<routeTables.Length; i++)
            {
                if (routeTables[i].MatchRequestedHost(host))
                {
                    return i;
                }
            }

            return -1;
        }

        public async Task<HandleResult> HandleRequest(HttpRequest httpReq, DateTime requestArrivedOn)
        {
            var requestedHost = httpReq.Host;
            if (string.IsNullOrEmpty(requestedHost))
            {
                return new HandleResult(-1, -1, BadRequest);
            }

            int routeTableIndex, endpointIndex;
            HttpResponse httpResp;

            if ((routeTableIndex = MatchRouteTable(requestedHost)) < 0)
            {
                endpointIndex = -1;
                httpResp = NotFound; // no matching host
            }
            else
            {
                RouteTable routeTable = routeTables[routeTableIndex];

                //TODO: httpReq = filterChain.FilterRequest(httpReq);

                IReadOnlyDictionary<string, string> pathVariables;
                if ((endpointIndex = routeTable.TryMatchEndpoint(httpReq.Method, httpReq.Url, out pathVariables)) < 0)
                {
                    httpResp = NotFound; // no matching route
                }
                else
                {
                    httpReq.PathVariables = pathVariables;

                    try
                    {
                        httpResp = await routeTable[endpointIndex].Handler(httpReq).ConfigureAwait(false);
                    }
                    catch (RequestException e)
                    {
                        httpResp = new ErrorResponse(e.HttpStatusCode, e);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error handling request:[{0}:{1}] - [{2}]: {3}", httpReq.Method, httpReq.Path, e.GetType().Name, e);
                        httpResp = new ErrorResponse(HttpStatusCode.InternalServerError, e);
                    }
                }

                //TODO: httpResp = routeTable.FilterResponse(httpReq, httpResp);

                if (endpointIndex >= 0)
                {
                    // int spins = 0;
                    while (!requestLogs.TryAdd(routeTableIndex, endpointIndex, httpReq, httpResp, requestArrivedOn, DateTime.UtcNow))
                    {
                        // spins++;
                        timer.Signal();
                        // await Task.Yield();
                    }

                    // if (spins > 0)
                    // {
                    //     logger.Warn("Incurred {0} spins to add request log", spins);
                    // }
                }
            }

            return new HandleResult(routeTableIndex, endpointIndex, httpResp);
        }
    }
}
