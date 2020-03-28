﻿using System;
using System.Collections.Generic;


namespace EddiInaraService
{
    public interface IInaraService
    {
        // Variables
        string commanderName { get; }
        string commanderFrontierID { get; }
        DateTime lastSync { get; }

        // Background Sync
        void Start(bool gameIsBeta = false, bool eddiIsBeta = false);
        void Stop();

        // API Event Queue Management
        void EnqueueAPIEvent(InaraAPIEvent inaraAPIEvent);
        void SendQueuedAPIEvents();
        List<InaraResponse> SendEventBatch(ref List<InaraAPIEvent> events, bool sendEvenForBetaGame = false);

        // Commander Profiles
        InaraCmdr GetCommanderProfile(string cmdrName);
        List<InaraCmdr> GetCommanderProfiles(string[] cmdrNames);
    }
}