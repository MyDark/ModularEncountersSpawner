﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using ModularEncountersSpawner;
using ModularEncountersSpawner.Configuration;
using ModularEncountersSpawner.Templates;
using ModularEncountersSpawner.Spawners;

namespace ModularEncountersSpawner {
    public static class KnownPlayerLocationManager {

        public static List<KnownPlayerLocation> Locations = new List<KnownPlayerLocation>();

        public static void AddKnownPlayerLocation(Vector3D coords, string faction, double radius, int duration = -1, int maxEncounters = -1) {

            bool foundExistingLocation = false;

            foreach(var location in Locations) {

                if(location.NpcFaction != faction) {

                    continue;

                }

                if(Vector3D.Distance(coords, location.Coords) > location.Radius) {

                    continue;

                }

                foundExistingLocation = true;
                break;

            }

            if(foundExistingLocation == false) {

                Locations.Add(new KnownPlayerLocation(faction, coords, radius, duration, maxEncounters));
                AlertPlayersOfNewKPL(coords, radius, faction);

            }

            SaveLocations();

        }

        public static void AlertPlayersOfNewKPL(Vector3D coords, double radius, string faction) {

            var playerList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(playerList);

            foreach(var player in playerList) {

                if(player.IsBot == true) {

                    continue;

                }

                if(Vector3D.Distance(player.GetPosition(), coords) > radius){

                    continue;

                }

                if(string.IsNullOrWhiteSpace(faction)) {

                    MyVisualScriptLogicProvider.ShowNotification("This area has been identified as \"Player Occupied\"", 5000, "Red", player.IdentityId);

                } else {

                    MyVisualScriptLogicProvider.ShowNotification("This area has been identified as \"Player Occupied\" by " + faction, 5000, "Red", player.IdentityId);

                }

            }

        }

        public static void CleanExpiredLocations() {

            bool needsUpdate = false;

            for(int i = Locations.Count - 1; i >= 0; i--) {

                var location = Locations[i];
                var duration = MyAPIGateway.Session.GameDateTime - location.LastSighting;

                if(location.ExpirationTimeMinutes >= 0 && duration.TotalMinutes >= location.ExpirationTimeMinutes) {

                    Locations.RemoveAt(i);
                    needsUpdate = true;
                    continue;

                }

                if(location.MaxSpawnedEncounters >= 0 && location.SpawnedEncounters >= location.MaxSpawnedEncounters) {

                    Locations.RemoveAt(i);
                    needsUpdate = true;
                    continue;

                }

            }

            if(needsUpdate == true) {

                SaveLocations();

            }

        }

        public static bool IsPositionInKnownPlayerLocation(Vector3D coords, bool matchFaction = false, string faction = "") {

            foreach(var location in Locations) {

                if (IsPositionInKnownPlayerLocation(location, coords, matchFaction, faction))
                    return true;

            }

            return false;

        }

        public static bool IsPositionInKnownPlayerLocation(KnownPlayerLocation location, Vector3D coords, bool matchFaction = false, string faction = "") {

            if (matchFaction == true && faction != location.NpcFaction) {

                return false;

            }

            if (Vector3D.Distance(coords, location.Coords) > location.Radius) {

                return false;

            }

            return true;

        }

        public static void IncreaseSpawnCountOfLocations(Vector3D coords) {

            foreach (var location in Locations) {

                if (IsPositionInKnownPlayerLocation(location, coords, false)) {

                    location.SpawnedEncounters++;

                }

            }

        }

        public static void LoadLocations() {

            try {

                string byteString = "";

                if(MyAPIGateway.Utilities.GetVariable<string>("MES-KnownPlayerLocationData", out byteString) == true) {

                    var byteData = Convert.FromBase64String(byteString);
                    Locations = MyAPIGateway.Utilities.SerializeFromBinary<List<KnownPlayerLocation>>(byteData);

                }

            } catch(Exception ex) {

                Logger.AddMsg("Error Retrieving KnownPlayerLocations Data from Sandbox");
                Logger.AddMsg(ex.ToString());
                Locations = new List<KnownPlayerLocation>();

            }

        }

        public static void SaveLocations() {

            try {

                var byteData = MyAPIGateway.Utilities.SerializeToBinary<List<KnownPlayerLocation>>(Locations);
                var byteString = Convert.ToBase64String(byteData);
                MyAPIGateway.Utilities.SetVariable<string>("MES-KnownPlayerLocationData", byteString);

            } catch(Exception ex){

                Logger.AddMsg("Error Saving KnownPlayerLocations Data to Sandbox");
                Logger.AddMsg(ex.ToString());

            }

        }

    }
}
