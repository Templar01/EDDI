﻿using Eddi;
using EddiCargoMonitor;
using EddiDataDefinitions;
using EddiEvents;
using EddiInaraService;
using EddiMissionMonitor;
using EddiShipMonitor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Controls;
using Utilities;

namespace EddiInaraResponder
{
    // Documentation: https://inara.cz/inara-api-docs/

    public class InaraResponder : EDDIResponder
    {
        private Thread updateThread;
        private bool bgSyncRunning;
        public string ResponderName()
        {
            return Properties.InaraResources.ResourceManager.GetString("name", CultureInfo.InvariantCulture);
        }

        public string LocalizedResponderName()
        {
            return Properties.InaraResources.name;
        }

        public string ResponderVersion()
        {
            return "1.0.0";
        }

        public string ResponderDescription()
        {
            return Properties.InaraResources.desc;
        }

        public InaraResponder()
        {
            Logging.Info("Initialised " + ResponderName() + " " + ResponderVersion());
        }

        public bool Start()
        {
            // Set up an event handler to send any pending events when the application exits.
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);

            Reload();
            return InaraService.Instance != null;
        }

        public void Reload()
        {
            Stop();
            InaraService.Reload();
            try
            {
                updateThread = new Thread(() => BackgroundSync())
                {
                    Name = "Inara sync",
                    IsBackground = true
                };
                updateThread.Start();
            }
            catch (ThreadAbortException tax)
            {
                Thread.ResetAbort();
                Logging.Debug("Thread aborted", tax);
            }
        }

        private void BackgroundSync()
        {
            bgSyncRunning = true;
            while (bgSyncRunning)
            {
                InaraService.Instance.SendQueuedAPIEventsAsync(EDDI.Instance.ShouldUseTestEndpoints());
                Thread.Sleep(120000);
            }
        }

        public UserControl ConfigurationTabItem()
        {
            return new ConfigurationWindow();
        }

        public void Stop()
        {
            bgSyncRunning = false;
            updateThread?.Abort();
            updateThread = null;
        }

        public void Handle(Event theEvent)
        {
            if (EDDI.Instance.inCQC)
            {
                // We don't do anything whilst in CQC
                return;
            }

            if (EDDI.Instance.inCrew)
            {
                // We don't do anything whilst in multicrew
                return;
            }

            if (EDDI.Instance.inBeta)
            {
                // We don't send data whilst in beta
                return;
            }

            InaraConfiguration inaraConfiguration = InaraConfiguration.FromFile();
            if (inaraConfiguration?.lastSync > theEvent.timestamp)
            {
                return;
            }

            if (!(theEvent is null))
            {
                try
                {
                    Logging.Debug("Handling event " + JsonConvert.SerializeObject(theEvent));

                    // These events will start or restart our instance of InaraService
                    if (theEvent is CommanderLoadingEvent commanderLoadingEvent)
                    {
                        handleCommanderLoadingEvent(commanderLoadingEvent);
                    }
                    else if (theEvent is CommanderStartedEvent commanderStartedEvent)
                    {
                        handleCommanderStartedEvent(commanderStartedEvent);
                    }
                    else if (InaraService.Instance != null)
                    {
                        if (theEvent is CommanderContinuedEvent commanderContinuedEvent)
                        {
                            handleCommanderContinuedEvent(commanderContinuedEvent);
                        }
                        else if (theEvent is CommanderProgressEvent commanderProgressEvent)
                        {
                            handleCommanderProgressEvent(commanderProgressEvent);
                        }
                        else if (theEvent is CommanderRatingsEvent commanderRatingsEvent)
                        {
                            handleCommanderRatingsEvent(commanderRatingsEvent);
                        }
                        else if (theEvent is EngineerProgressedEvent engineerProgressedEvent)
                        {
                            handleEngineerProgressedEvent(engineerProgressedEvent);
                        }
                        else if (theEvent is StatisticsEvent statisticsEvent)
                        {
                            handleStatisticsEvent(statisticsEvent);
                        }
                        else if (theEvent is PowerplayEvent powerplayEvent)
                        {
                            handlePowerplayEvent(powerplayEvent);
                        }
                        else if (theEvent is PowerLeftEvent powerLeftEvent)
                        {
                            handlePowerLeftEvent(powerLeftEvent);
                        }
                        else if (theEvent is PowerJoinedEvent powerJoinedEvent)
                        {
                            handlePowerJoinedEvent(powerJoinedEvent);
                        }
                        else if (theEvent is CommanderReputationEvent commanderReputationEvent)
                        {
                            handleCommanderReputationEvent(commanderReputationEvent);
                        }
                        else if (theEvent is JumpedEvent jumpedEvent)
                        {
                            handleJumpedEvent(jumpedEvent);
                        }
                        else if (theEvent is LocationEvent locationEvent)
                        {
                            handleLocationEvent(locationEvent);
                        }
                        else if (theEvent is CargoEvent cargoEvent)
                        {
                            handleCargoEvent(cargoEvent);
                        }
                        else if (theEvent is CommodityCollectedEvent commodityCollectedEvent)
                        {
                            handleCommodityCollectedEvent(commodityCollectedEvent);
                        }
                        else if (theEvent is CommodityEjectedEvent commodityEjectedEvent)
                        {
                            handleCommodityEjectedEvent(commodityEjectedEvent);
                        }
                        else if (theEvent is CommodityPurchasedEvent commodityPurchasedEvent)
                        {
                            handleCommodityPurchasedEvent(commodityPurchasedEvent);
                        }
                        else if (theEvent is CommodityRefinedEvent commodityRefinedEvent)
                        {
                            handleCommodityRefinedEvent(commodityRefinedEvent);
                        }
                        else if (theEvent is CommoditySoldEvent commoditySoldEvent)
                        {
                            handleCommoditySoldEvent(commoditySoldEvent);
                        }
                        else if (theEvent is CargoDepotEvent cargoDepotEvent)
                        {
                            handleCargoDepotEvent(cargoDepotEvent);
                        }
                        else if (theEvent is DiedEvent diedEvent)
                        {
                            handleDiedEvent(diedEvent);
                        }
                        else if (theEvent is EngineerContributedEvent engineerContributedEvent)
                        {
                            handleEngineerContributedEvent(engineerContributedEvent);
                        }
                        else if (theEvent is SearchAndRescueEvent searchAndRescueEvent)
                        {
                            handleSearchAndRescueEvent(searchAndRescueEvent);
                        }
                        else if (theEvent is MissionCompletedEvent missionCompletedEvent)
                        {
                            handleMissionCompletedEvent(missionCompletedEvent);
                        }
                        else if (theEvent is MaterialInventoryEvent materialInventoryEvent)
                        {
                            handleMaterialInventoryEvent(materialInventoryEvent);
                        }
                        else if (theEvent is MaterialCollectedEvent materialCollectedEvent)
                        {
                            handleMaterialCollectedEvent(materialCollectedEvent);
                        }
                        else if (theEvent is MaterialDiscardedEvent materialDiscardedEvent)
                        {
                            handleMaterialDiscardedEvent(materialDiscardedEvent);
                        }
                        else if (theEvent is MaterialDonatedEvent materialDonatedEvent)
                        {
                            handleMaterialDonatedEvent(materialDonatedEvent);
                        }
                        else if (theEvent is MaterialTradedEvent materialTradedEvent)
                        {
                            handleMaterialTradedEvent(materialTradedEvent);
                        }
                        else if (theEvent is SynthesisedEvent synthesisedEvent)
                        {
                            handleSynthesisedEvent(synthesisedEvent);
                        }
                        else if (theEvent is ModificationCraftedEvent modificationCraftedEvent)
                        {
                            handleModificationCraftedEvent(modificationCraftedEvent);
                        }
                        else if (theEvent is TechnologyBrokerEvent technologyBrokerEvent)
                        {
                            handleTechnologyBrokerEvent(technologyBrokerEvent);
                        }
                        else if (theEvent is StoredModulesEvent storedModulesEvent)
                        {
                            handleStoredModulesEvent(storedModulesEvent);
                        }
                        else if (theEvent is ShipPurchasedEvent shipPurchasedEvent)
                        {
                            handleShipPurchasedEvent(shipPurchasedEvent);
                        }
                        else if (theEvent is ShipDeliveredEvent shipDeliveredEvent)
                        {
                            handleShipDeliveredEvent(shipDeliveredEvent);
                        }
                        else if (theEvent is ShipSoldEvent shipSoldEvent)
                        {
                            handleShipSoldEvent(shipSoldEvent);
                        }
                        else if (theEvent is ShipSoldOnRebuyEvent shipSoldOnRebuyEvent)
                        {
                            handleShipSoldOnRebuyEvent(shipSoldOnRebuyEvent);
                        }
                        else if (theEvent is ShipSwappedEvent shipSwappedEvent)
                        {
                            handleShipSwappedEvent(shipSwappedEvent);
                        }
                        else if (theEvent is ShipLoadoutEvent shipLoadoutEvent)
                        {
                            handleShipLoadoutEvent(shipLoadoutEvent);
                        }
                        else if (theEvent is ShipRenamedEvent shipRenamedEvent)
                        {
                            handleShipRenamedEvent(shipRenamedEvent);
                        }
                        else if (theEvent is ShipTransferInitiatedEvent shipTransferInitiatedEvent)
                        {
                            handleShipTransferInitiatedEvent(shipTransferInitiatedEvent);
                        }
                        else if (theEvent is DockedEvent dockedEvent)
                        {
                            handleDockedEvent(dockedEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>
                        {
                            { "event", JsonConvert.SerializeObject(theEvent) },
                            { "exception", ex.Message },
                            { "stacktrace", ex.StackTrace }
                        };
                    Logging.Error("Failed to handle event " + theEvent.type, data);
                }
            }
        }

        private void handleDockedEvent(DockedEvent @event)
        {
            // Don't add this at the session start after Location, per guidance from Inara. 
            if (@event.station != firstDockedLocation)
            {
                Ship currentShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetCurrentShip();
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderTravelDock", new Dictionary<string, object>()
                {
                    { "starsystemName", @event.system },
                    { "stationName", @event.station },
                    { "marketID", @event.marketId },
                    { "shipType", currentShip.model },
                    { "shipGameID", currentShip.LocalId }
                }));
            }
            firstDockedLocation = null;
        }

        private void handleShipTransferInitiatedEvent(ShipTransferInitiatedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShipTransfer", new Dictionary<string, object>()
            {
                { "shipType", @event.ship },
                { "shipGameID", @event.shipid },
                { "starsystemName", EDDI.Instance.CurrentStarSystem?.systemname },
                { "stationName", EDDI.Instance.CurrentStation?.name },
                { "marketID", EDDI.Instance.CurrentStation?.marketId },
                { "transferTime", @event.time }
            }));
        }

        private void handleShipRenamedEvent(ShipRenamedEvent @event)
        {
            Ship currentShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetShip(@event.shipid);
            Dictionary<string, object> currentShipData = new Dictionary<string, object>()
            {
                { "shipType", currentShip.EDName },
                { "shipGameID", currentShip.LocalId },
                { "shipName", currentShip.name },
                { "shipIdent", currentShip.ident },
                { "isHot", currentShip.hot },
                { "isCurrentShip", true }
            };
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShip", currentShipData));
        }

        private void handleShipLoadoutEvent(ShipLoadoutEvent @event)
        {
            Ship currentShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetShip(@event.shipid);
            Dictionary<string, object> currentShipData = new Dictionary<string, object>()
            {
                { "shipType", currentShip.EDName },
                { "shipGameID", currentShip.LocalId },
                { "shipName", currentShip.name },
                { "shipIdent", currentShip.ident },
                { "isHot", currentShip.hot },
                { "isCurrentShip", true },
                { "shipHullValue", @event.hullvalue },
                { "shipModulesValue", @event.modulesvalue },
                { "shipRebuyCost", @event.rebuy }
            };
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShip", currentShipData));
            IDictionary<string, object> rawShipObj = Deserializtion.DeserializeData(@event.raw);
            rawShipObj.TryGetValue("Modules", out object modulesVal);
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShipLoadout", new Dictionary<string, object>()
            {
                { "shipType", @event.ship },
                { "shipGameID", @event.shipid },
                { "shipLoadout", modulesVal }
            }));
        }

        private void handleShipSwappedEvent(ShipSwappedEvent @event)
        {
            if (!string.IsNullOrEmpty(@event.storedship))
            {
                Ship storedShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetShip(@event.storedshipid);
                Dictionary<string, object> storedShipData = new Dictionary<string, object>()
                {
                    { "shipType", storedShip.EDName },
                    { "shipGameID", storedShip.LocalId },
                    { "shipName", storedShip.name },
                    { "shipIdent", storedShip.ident },
                    { "isHot", storedShip.hot },
                    { "isCurrentShip", true },
                    { "starsystemName", EDDI.Instance.CurrentStarSystem?.systemname },
                    { "stationName", EDDI.Instance.CurrentStation?.name },
                    { "marketID", EDDI.Instance.CurrentStation?.marketId }
                };
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShip", storedShipData));
            }
            else if (!string.IsNullOrEmpty(@event.soldship))
            {
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderShip", new Dictionary<string, object>()
                {
                    { "shipType", @event.storedship },
                    { "shipGameID", @event.storedshipid }
                }));
            }
            Ship currentShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetShip(@event.shipid);
            Dictionary<string, object> currentShipData = new Dictionary<string, object>()
            {
                { "shipType", currentShip.EDName },
                { "shipGameID", currentShip.LocalId },
                { "shipName", currentShip.name },
                { "shipIdent", currentShip.ident },
                { "isHot", currentShip.hot },
                { "isCurrentShip", true }
            };
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShip", currentShipData));
        }

        private void handleShipSoldOnRebuyEvent(ShipSoldOnRebuyEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderShip", new Dictionary<string, object>()
            {
                { "shipType", @event.ship },
                { "shipGameID", @event.shipid }
            }));
        }

        private void handleShipSoldEvent(ShipSoldEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderShip", new Dictionary<string, object>()
            {
                { "shipType", @event.ship },
                { "shipGameID", @event.shipid }
            }));
        }

        private void handleShipDeliveredEvent(ShipDeliveredEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderShip", new Dictionary<string, object>()
            {
                { "shipType", @event.ship },
                { "shipGameID", @event.shipid }
            }));
        }

        private void handleShipPurchasedEvent(ShipPurchasedEvent @event)
        {
            if (!string.IsNullOrEmpty(@event.storedship))
            {
                Ship storedShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetShip(@event.storedshipid);
                Dictionary<string, object> storedShipData = new Dictionary<string, object>()
                {
                    { "shipType", storedShip.EDName },
                    { "shipGameID", storedShip.LocalId },
                    { "shipName", storedShip.name },
                    { "shipIdent", storedShip.ident },
                    { "isHot", storedShip.hot },
                    { "isCurrentShip", true },
                    { "starsystemName", EDDI.Instance.CurrentStarSystem?.systemname },
                    { "stationName", EDDI.Instance.CurrentStation?.name },
                    { "marketID", EDDI.Instance.CurrentStation?.marketId }
                };
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderShip", storedShipData));
            }
            else if (!string.IsNullOrEmpty(@event.soldship))
            {
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderShip", new Dictionary<string, object>()
                {
                    { "shipType", @event.storedship },
                    { "shipGameID", @event.storedshipid }
                }));
            }
        }

        private void handleStoredModulesEvent(StoredModulesEvent @event)
        {
            List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>();
            foreach (StoredModule storedModule in @event.storedmodules)
            {
                eventData.Add(new Dictionary<string, object>()
                {
                    { "itemName", storedModule?.module?.edname },
                    { "itemValue", storedModule?.module?.value },
                    { "isHot", storedModule?.module?.hot },
                    { "starsystemName", storedModule.system },
                    { "stationName", storedModule?.station },
                    { "marketID", storedModule?.marketid },
                    { "engineering", storedModule.rawEngineering }
                });                
            }
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderStorageModules", eventData));
        }

        private void handleMaterialInventoryEvent(MaterialInventoryEvent @event)
        {
            List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>();
            foreach (MaterialAmount materialAmount in @event.inventory)
            {
                eventData.Add(new Dictionary<string, object>()
                {
                    { "itemName", materialAmount?.edname },
                    { "itemCount", materialAmount.amount }
                });
            }
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderInventoryMaterials", eventData));
        }

        private void handleModificationCraftedEvent(ModificationCraftedEvent @event)
        {
            if (@event.materials?.Count > 0)
            {
                foreach (MaterialAmount materialAmount in @event.materials)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
                    {
                        { "itemName", materialAmount?.edname },
                        { "itemCount", materialAmount.amount }
                    }));
                }
            }
            if (@event.commodities?.Count > 0)
            {
                foreach (CommodityAmount commodityAmount in @event.commodities)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
                    {
                        { "itemName", commodityAmount?.commodityDefinition?.edname },
                        { "itemCount", commodityAmount.amount }
                    }));
                }
            }
        }

        private void handleSynthesisedEvent(SynthesisedEvent @event)
        {
            if (@event.materials?.Count > 0)
            {
                foreach (MaterialAmount materialAmount in @event.materials)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
                    {
                        { "itemName", materialAmount?.edname },
                        { "itemCount", materialAmount.amount }
                    }));
                }
            }
        }

        private void handleMaterialDonatedEvent(MaterialDonatedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
            {
                { "itemName", @event.edname },
                { "itemCount", @event.amount }
            }));
        }

        private void handleMaterialDiscardedEvent(MaterialDiscardedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
            {
                { "itemName", @event.edname },
                { "itemCount", @event.amount }
            }));
        }

        private void handleMaterialTradedEvent(MaterialTradedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
            {
                { "itemName", @event.paid_edname },
                { "itemCount", @event.paid_quantity }
            }));
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryMaterialsItem", new Dictionary<string, object>()
            {
                { "itemName", @event.received_edname },
                { "itemCount", @event.received_quantity }
            }));
        }

        private void handleTechnologyBrokerEvent(TechnologyBrokerEvent @event)
        {
            if (@event.materials?.Count > 0)
            {
                foreach (MaterialAmount materialAmount in @event.materials)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
                    {
                        { "itemName", materialAmount?.edname },
                        { "itemCount", materialAmount.amount }
                    }));
                }
            }
            if (@event.commodities?.Count > 0)
            {
                foreach (CommodityAmount commodityAmount in @event.commodities)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
                    {
                        { "itemName", commodityAmount?.commodityDefinition?.edname },
                        { "itemCount", commodityAmount.amount }
                    }));
                }
            }
        }

        private void handleMaterialCollectedEvent(MaterialCollectedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryMaterialsItem", new Dictionary<string, object>()
            {
                { "itemName", @event.edname },
                { "itemCount", @event.amount }
            }));
        }

        private void handleCargoEvent(CargoEvent @event)
        {
            List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>();
            foreach (CargoInfo cargoInfo in @event.inventory)
            {
                eventData.Add(new Dictionary<string, object>()
                {
                    { "itemName", cargoInfo.name },
                    { "itemCount", cargoInfo.count }
                });
            }
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderInventoryCargo", eventData));
        }

        private void handleDiedEvent(DiedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderInventoryCargo", new List<Dictionary<string, object>>()));
        }

        private void handleSearchAndRescueEvent(SearchAndRescueEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
            {
                { "itemName", @event.commodity?.invariantName },
                { "itemCount", @event.amount }
            }));
        }

        private void handleEngineerContributedEvent(EngineerContributedEvent @event)
        {
            if (@event.contributiontype == "Commodity")
            {
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
                {
                    { "itemName", @event.commodityAmount?.commodityDefinition?.edname },
                    { "itemCount", @event.amount }
                }));
            }
            else if (@event.contributiontype == "Material")
            {
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryMaterialsItem", new Dictionary<string, object>()
                {
                    { "itemName", @event.materialAmount?.edname },
                    { "itemCount", @event.amount }
                }));
            }
        }

        private void handleCommoditySoldEvent(CommoditySoldEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
            {
                { "itemName", @event.commodityDefinition?.edname },
                { "itemCount", @event.amount },
                { "isStolen", @event.stolen }
            }));
        }

        private void handleCommodityEjectedEvent(CommodityEjectedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
            {
                { "itemName", @event.commodityDefinition?.edname },
                { "itemCount", @event.amount },
                { "missionGameID", @event.missionid }
            }));
        }

        private void handleCargoDepotEvent(CargoDepotEvent @event)
        {
            if (@event.updatetype == "Collect")
            {
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryCargoItem", new Dictionary<string, object>()
                {
                    { "itemName", @event.commodityDefinition?.edname },
                    { "itemCount", @event.amount },
                    { "isStolen", false },
                    { "missionGameID", @event.missionid }
                }));
            }
            else if (@event.updatetype == "Deliver")
            {
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "delCommanderInventoryCargoItem", new Dictionary<string, object>()
                {
                    { "itemName", @event.commodityDefinition?.edname },
                    { "itemCount", @event.amount },
                    { "isStolen", false },
                    { "missionGameID", @event.missionid }
                }));
            }
        }

        private void handleCommodityRefinedEvent(CommodityRefinedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryCargoItem", new Dictionary<string, object>()
            {
                { "itemName", @event.commodityDefinition?.edname },
                { "itemCount", 1 },
                { "isStolen", false }
            }));
        }

        private void handleCommodityPurchasedEvent(CommodityPurchasedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryCargoItem", new Dictionary<string, object>()
            {
                { "itemName", @event.commodityDefinition?.edname },
                { "itemCount", @event.amount },
                { "isStolen", false }
            }));
        }

        private void handleCommodityCollectedEvent(CommodityCollectedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryCargoItem", new Dictionary<string, object>()
            {
                { "itemName", @event.commodityDefinition?.edname },
                { "itemCount", 1 },
                { "isStolen", @event.stolen },
                { "missionGameID", @event.missionid }
            }));
        }

        private void handleLocationEvent(LocationEvent @event)
        {
            List<Dictionary<string, object>> minorFactionData = minorFactionReputations(@event.factions);
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderReputationMinorFaction", minorFactionData));
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderTravelLocation", new Dictionary<string, object>()
            {
                { "starsystemName", @event.systemname },
                { "stationName", @event.station },
                { "marketID", @event.marketId }
            }));
            if (@event.docked)
            {
                // Set our docked lcoation for reference by the `Docked` event.
                firstDockedLocation = @event.station;
            }
        }
        string firstDockedLocation = null;

        private void handleJumpedEvent(JumpedEvent @event)
        {
            List<Dictionary<string, object>> minorFactionData = minorFactionReputations(@event.factions);
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderReputationMinorFaction", minorFactionData));

            Ship currentShip = ((ShipMonitor)EDDI.Instance.ObtainMonitor("Ship Monitor")).GetCurrentShip();
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderTravelFSDJump", new Dictionary<string, object>()
            {
                { "starsystemName", @event.system },
                { "jumpDistance", @event.distance },
                { "shipType", currentShip.model },
                { "shipGameID", currentShip.LocalId }
            }));
        }

        private static List<Dictionary<string, object>> minorFactionReputations(List<Faction> factions)
        {
            // Reputation progress in a range: [-1..1], which corresponds to a reputation range from -100% (hostile) to 100% (allied).
            List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>();
            foreach (Faction faction in factions)
            {
                eventData.Add(new Dictionary<string, object>()
                {
                    { "minorfactionName", faction?.name },
                    { "minorfactionReputation", (decimal)(faction?.myreputation / 100) }
                });
            }
            return eventData;
        }

        private void handleCommanderReputationEvent(CommanderReputationEvent @event)
        {
            // Reputation progress in a range: [-1..1], which corresponds to a reputation range from -100% (hostile) to 100% (allied).
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderReputationMajorFaction", new List<Dictionary<string, object>>()
            {
                {
                    new Dictionary<string, object>()
                    {
                        { "majorfactionName", "empire" },
                        { "majorfactionReputation", @event.empire / 100 }
                    }
                },
                {
                    new Dictionary<string, object>()
                    {
                        { "majorfactionName", "federation" },
                        { "majorfactionReputation", @event.federation / 100 }
                    }
                },
                {
                    new Dictionary<string, object>()
                    {
                        { "majorfactionName", "independent" },
                        { "majorfactionReputation", @event.independent / 100 }
                    }
                },
                {
                    new Dictionary<string, object>()
                    {
                        { "majorfactionName", "alliance" },
                        { "majorfactionReputation", @event.alliance / 100 }
                    }
                }
            }));
        }

        private void handlePowerJoinedEvent(PowerJoinedEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankPower", new Dictionary<string, object>()
            {
                { "powerName", @event.Power?.invariantName },
                { "rankValue", 1 }
            }));
        }

        private void handlePowerLeftEvent(PowerLeftEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankPower", new Dictionary<string, object>()
            {
                { "powerName", @event.Power?.invariantName },
                { "rankValue", 0 }
            }));
        }

        private void handlePowerplayEvent(PowerplayEvent @event)
        {
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankPower", new Dictionary<string, object>()
            {
                { "powerName", @event.Power?.invariantName },
                { "rankValue", @event.rank }
            }));
        }

        private void handleCommanderProgressEvent(CommanderProgressEvent @event)
        {
            // Pilots federation/Navy rank name as are in the journals (["combat", "trade", "explore", "cqc", "federation", "empire"]) 
            // Rank progress (range: [0..1], which corresponds to 0% - 100%) (In the journal, these are given out of 100)
            List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>()
            {
                new Dictionary<string, object>()
                {
                    { "rankName", "combat" },
                    { "rankProgress", @event.combat / 100 }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "trade" },
                    { "rankProgress", @event.trade / 100 }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "explore" },
                    { "rankProgress", @event.exploration / 100 }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "empire" },
                    { "rankProgress", @event.empire / 100 }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "federation" },
                    { "rankProgress", @event.federation / 100 }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "cqc" },
                    { "rankProgress", @event.cqc / 100 }
                }
            };
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankPilot", eventData));
        }

        private void handleCommanderRatingsEvent(CommanderRatingsEvent @event)
        {
            // Pilots federation/Navy rank name as are in the journals (["combat", "trade", "explore", "cqc", "federation", "empire"]) 
            // Rank value (range [0..8] for Pilots federation ranks, range [0..14] for Navy ranks)
            List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>()
            {
                new Dictionary<string, object>()
                {
                    { "rankName", "combat" },
                    { "rankValue", @event.combat?.rank }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "trade" },
                    { "rankValue", @event.trade?.rank }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "explore" },
                    { "rankValue", @event.exploration?.rank }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "empire" },
                    { "rankValue", @event.empire?.rank }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "federation" },
                    { "rankValue", @event.federation?.rank }
                },
                new Dictionary<string, object>()
                {
                    { "rankName", "cqc" },
                    { "rankValue", @event.cqc?.rank }
                }
            };
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankPilot", eventData));
        }

        private void handleEngineerProgressedEvent(EngineerProgressedEvent @event)
        {
            // Send engineer rank progress to Inara
            IDictionary<string, object> data = Deserializtion.DeserializeData(@event.raw);
            data.TryGetValue("Engineers", out object val);
            if (val != null)
            {
                // This is a startup entry, containing data about all known engineers
                List<Dictionary<string, object>> eventData = new List<Dictionary<string, object>>();
                List<object> engineers = (List<object>)val;
                foreach (IDictionary<string, object> engineerData in engineers)
                {
                    Dictionary<string, object> engineer = parseEngineerInara(engineerData);

                    eventData.Add(engineer);
                }
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankEngineer", eventData));
            }
            else
            {
                // This is a progress entry, containing data about a single engineer
                Dictionary<string, object> eventData = parseEngineerInara(data);
                InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderRankEngineer", eventData));
            }
        }

        private static Dictionary<string, object> parseEngineerInara(IDictionary<string, object> engineerData)
        {
            Dictionary<string, object> engineer = new Dictionary<string, object>()
            {
                { "engineerName", JsonParsing.getString(engineerData, "Engineer") },
                { "rankStage", JsonParsing.getString(engineerData, "Progress") }
            };
            int? rank = JsonParsing.getOptionalInt(engineerData, "Rank");
            if (!(rank is null))
            {
                engineer.Add("rankValue", rank);
            }
            return engineer;
        }

        private void handleStatisticsEvent(StatisticsEvent @event)
        {
            // Send the commanders game statistics to Inara
            // Prepare and send the raw event, less the event name and timestamp. Please note that the statistics 
            // are always overridden as a whole, so any partial updates will cause erasing of the rest.
            IDictionary<string, object> data = Deserializtion.DeserializeData(@event.raw);
            data.Remove("timestamp");
            data.Remove("event");
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderGameStatistics", (Dictionary<string, object>)data));
        }

        private void handleCommanderContinuedEvent(CommanderContinuedEvent @event)
        {
            // Sets current credits and loans. A record is added to the credits log (if the value differs).
            // Warning: Do NOT set credits/assets unless you are absolutely sure they are correct. 
            // The journals currently doesn't contain crew wage cuts, so credit gains are very probably off 
            // for most of the players. Also, please, do not send each minor credits change, as it will 
            // spam player's credits log with unusable data and they won't be most likely very happy about it. 
            // It may be good to set credits just on the session start, session end and on the big changes 
            // or in hourly intervals.
            InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "setCommanderCredits", new Dictionary<string, object>()
            {
                { "commanderCredits", @event.credits },
                { "commanderLoan", @event.loan }
            }));
        }

        private void handleCommanderStartedEvent(CommanderStartedEvent @event)
        {
            // Start or restart the Inara service
            InaraConfiguration inaraConfiguration = InaraConfiguration.FromFile();
            inaraConfiguration.commanderName = @event.name;
            inaraConfiguration.commanderFrontierID = @event.frontierID;
            inaraConfiguration.ToFile();
            if (inaraConfiguration.commanderFrontierID != InaraService.Instance.commanderFrontierID)
            {
                InaraService.Reload();
            }
        }

        private void handleCommanderLoadingEvent(CommanderLoadingEvent @event)
        {
            // Start or restart the Inara service
            InaraConfiguration inaraConfiguration = InaraConfiguration.FromFile();
            inaraConfiguration.commanderName = @event.name;
            inaraConfiguration.commanderFrontierID = @event.frontierID;
            inaraConfiguration.ToFile();
            if (inaraConfiguration.commanderFrontierID != InaraService.Instance.commanderFrontierID)
            {
                InaraService.Reload();
            }
        }

        private void handleMissionCompletedEvent(MissionCompletedEvent @event)
        {
            // Adds star system permit for the commander. You do not need to handle permits granted for the 
            // Pilots Federation or Navy rank promotion, but you should handle any other ways (like mission 
            // rewards).
            if (@event.permitsawarded.Count > 0)
            {
                Dictionary<string, object> eventData = new Dictionary<string, object>();
                foreach (string systemName in @event.permitsawarded)
                {
                    if (string.IsNullOrEmpty(systemName)) { continue; }
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderPermit", new Dictionary<string, object>() { { "starsystemName", systemName } }));
                }
            }
            if (@event.materialsrewards?.Count > 0)
            {
                foreach (MaterialAmount materialAmount in @event.materialsrewards)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryMaterialsItem", new Dictionary<string, object>()
                    {
                        { "itemName", materialAmount?.edname },
                        { "itemCount", materialAmount.amount },
                        { "isStolen", false },
                        { "missionGameID", @event.missionid }
                    }));
                }
            }
            if (@event.commodityrewards?.Count > 0)
            {
                foreach (CommodityAmount commodityAmount in @event.commodityrewards)
                {
                    InaraService.Instance.EnqueueAPIEvent(new InaraAPIEvent(@event.timestamp, "addCommanderInventoryCargoItem", new Dictionary<string, object>()
                    {
                        { "itemName", commodityAmount?.commodityDefinition?.edname },
                        { "itemCount", commodityAmount.amount },
                        { "isStolen", false },
                        { "missionGameID", @event.missionid }
                    }));
                }
            }
        }

        void OnApplicationExit(object sender, EventArgs e)
        {
            InaraService.Instance.SendQueuedAPIEventsAsync(EDDI.Instance.ShouldUseTestEndpoints());
        }
    }
}
