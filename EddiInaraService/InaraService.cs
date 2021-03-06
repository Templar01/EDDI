﻿using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utilities;

namespace EddiInaraService
{
    public partial class InaraService
    {
        // API Documenation: https://inara.cz/inara-api-docs/

        public string commanderName { get; private set; }
        public string commanderFrontierID { get; private set; }
        private string apiKey { get; set; }
        public DateTime lastSync { get; set; }

        private const string readonlyAPIkey = "9efrgisivgw8kksoosowo48kwkkw04skwcgo840";

        private ConcurrentQueue<InaraAPIEvent> queuedAPIEvents { get; set; } = new ConcurrentQueue<InaraAPIEvent>();

        private static readonly object instanceLock = new object();

        private static InaraService instance;
        public static InaraService Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                        {
                            instance = new InaraService();
                        }
                    }
                }
                return instance;
            }
        }

        private static bool eddiInBeta;
        private static bool gameInBeta;

        public static void Start(bool gameIsBeta = false, bool eddiIsBeta = false)
        {
            Logging.Debug("Creating new Inara service instance.");
            eddiInBeta = eddiIsBeta;
            gameInBeta = gameIsBeta;
            _ = Instance;
        }

        protected InaraService()
        {
            // Set up the Inara service
            InaraConfiguration inaraCredentials = InaraConfiguration.FromFile();
            if (inaraCredentials == null) { return; }

            // commanderName: In-game CMDR name of user (not set by user, get this from journals or 
            // cAPI to ensure it is a correct in-game name to avoid future problems). It is recommended 
            // to be always set when no generic API key is used (otherwise some events may not work).
            commanderName = inaraCredentials.commanderName;

            // commanderFrontierID: Commander's unique Frontier ID (is provided by journals since 3.3)
            // in the format: 'F123456'. When not known, set nothing.
            commanderFrontierID = inaraCredentials.commanderFrontierID;

            lastSync = inaraCredentials.lastSync;
            apiKey = inaraCredentials.apiKey;
            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(commanderName))
            {
                // fully configured
                Logging.Info("Configuring EDDI access to Inara profile data");
            }
            else
            {
                apiKey = readonlyAPIkey;
                if (string.IsNullOrEmpty(inaraCredentials.apiKey))
                {
                    Logging.Info("Configuring Inara service for limited access: API key not set.");
                }

                if (string.IsNullOrEmpty(commanderName))
                {
                    Logging.Info("Configuring Inara service for limited access: Commander name not detected.");
                }
            }
        }

        // If you need to do some testing on Inara's API, please set the `isDeveloped` boolean header property to true.
        public List<InaraResponse> SendEventBatch(ref List<InaraAPIEvent> events, bool sendEvenForBetaGame = false)
        {
            if (!sendEvenForBetaGame && gameInBeta) { return null; }

            List<InaraResponse> inaraResponses = new List<InaraResponse>();

            if (string.IsNullOrEmpty(apiKey))
            {
                return inaraResponses;
            }

            // Flag each event with a unique ID we can use when processing responses
            List<InaraAPIEvent> indexedEvents = new List<InaraAPIEvent>();
            for (int i = 0; i < events.Count; i++)
            {
                InaraAPIEvent indexedEvent = events[i];
                indexedEvent.eventCustomID = i;
                indexedEvents.Add(indexedEvent);
            }

            var client = new RestClient("https://inara.cz/inapi/v1/");
            var request = new RestRequest(Method.POST);
            InaraSendJson inaraRequest = new InaraSendJson()
            {
                header = new Dictionary<string, object>()
                {
                    // Per private conversation with Artie and per Inara API docs, the `isDeveloped` property
                    // should (counterintuitively) be set to true when the an application is in development.
                    // Quote: `it's "true" because the app "is (being) developed"`
                    // Quote: `isDeveloped is meant as "the app is currently being developed and may be broken`
                    { "appName", "EDDI" },
                    { "appVersion", Constants.EDDI_VERSION.ToString() },
                    { "isDeveloped", eddiInBeta },
                    { "commanderName", commanderName ?? (eddiInBeta ? "TestCmdr" : null) },
                    { "commanderFrontierID", commanderFrontierID },
                    { "APIkey", apiKey }
                },
                events = indexedEvents
            };
            request.RequestFormat = DataFormat.Json;
            request.AddBody(inaraRequest); // uses JsonSerializer

            Logging.Debug("Sending to Inara: " + client.BuildUri(request).AbsoluteUri);
            var clientResponse = client.Execute<InaraResponses>(request);
            if (clientResponse.IsSuccessful)
            {
                InaraResponses response = clientResponse.Data;
                if (validateResponse(response.header, ref indexedEvents))
                {
                    foreach (InaraResponse inaraResponse in response.events)
                    {
                        if (validateResponse(inaraResponse, ref indexedEvents))
                        {
                            inaraResponses.Add(inaraResponse);
                        }
                    }
                }
                return inaraResponses;
            }
            else
            {
                Logging.Warn("Unable to connect to the Inara server.", clientResponse.ErrorMessage);
                foreach (InaraAPIEvent inaraAPIEvent in events)
                {
                    // Re-enqueue and send at a later time.
                    EnqueueAPIEvent(inaraAPIEvent);
                }
                return null;
            }
        }

        private static bool validateResponse(InaraResponse inaraResponse, ref List<InaraAPIEvent> indexedEvents)
        {
            if (inaraResponse.eventStatus == 200)
            {
                // 200 - Ok
                return true;
            }
            else
            {
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    { "InaraAPIEvent", indexedEvents.Find(e => e.eventCustomID == inaraResponse.eventCustomID) },
                    { "InaraResponse", inaraResponse }
                };
                if (inaraResponse.eventStatus == 400)
                {
                    // 400 - Error (you probably did something wrong, there are properties missing, etc. The event was skipped or whole batch cancelled on failed authorization.)
                    Logging.Error("Inara responded with: " + inaraResponse.eventStatusText, JsonConvert.SerializeObject(data));
                }
                else
                {
                    // 202 - Warning (everything is OK, but there may be multiple results for the input properties, etc.)
                    // 204 - 'Soft' error (everything was formally OK, but there are no results for the properties set, etc.)
                    // Inara may also return null as it undergoes a nightly manintenance cycle where the servers go offline temporarily.
                    Logging.Warn("Inara responded with: " + (inaraResponse.eventStatusText ?? "(No response)"), JsonConvert.SerializeObject(data));

                }
                return false;
            }
        }

        public async void SendQueuedAPIEventsAsync()
        {
            List<InaraAPIEvent> queue = new List<InaraAPIEvent>();
            while (Instance.queuedAPIEvents.TryDequeue(out InaraAPIEvent pendingEvent))
            {
                queue.Add(pendingEvent);
            }
            if (queue.Count > 0)
            {
                await Task.Run(() => Instance.SendEventBatch(ref queue));
                Instance.lastSync = queue.Max(e => e.eventTimeStamp);
                InaraConfiguration inaraConfiguration = InaraConfiguration.FromFile();
                inaraConfiguration.lastSync = Instance.lastSync;
                inaraConfiguration.ToFile();
            }
        }

        public void EnqueueAPIEvent(InaraAPIEvent inaraAPIEvent)
        {
            if (!(inaraAPIEvent is null) && lastSync < inaraAPIEvent.eventTimeStamp)
            {
                Instance.queuedAPIEvents.Enqueue(inaraAPIEvent);
            }
        }
    }

    internal class InaraSendJson
    {
        public Dictionary<string, object> header { get; set; }

        public List<InaraAPIEvent> events { get; set; }
    }

    internal class InaraResponses
    {
        public InaraResponse header { get; set; }

        public List<InaraResponse> events { get; set; }
    }

    public class InaraResponse
    {
        public int eventStatus { get; set; }

        public string eventStatusText { get; set; }

        public object eventData { get; set; }

        public int eventCustomID { get; set; } // Optional index
    }
}
