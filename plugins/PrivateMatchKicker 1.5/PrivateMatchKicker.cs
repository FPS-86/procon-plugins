using System;
using System.Collections.Generic;
using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin;

namespace PRoConEvents
{
    public class PrivateMatchKicker : PRoConPluginAPI, IPRoConPluginInterface
    {
        private HashSet<string> reservedPlayers = null;

        private bool ignoreCase = false;

        public string GetPluginName()
        {
            return "Private Match Kicker";
        }

        public string GetPluginVersion()
        {
            return "1.5.0.0";
        }

        public string GetPluginAuthor()
        {
            return "aidinabedi";
        }

        public string GetPluginWebsite()
        {
            return "";
        }

        public string GetPluginDescription()
        {
            return @"Kicks a player if they are not included in the reserved slots list (see Lists tab).";
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            return GetPluginVariables();
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return new List<CPluginVariable>
            {
                new CPluginVariable("Case Insensitive Comparison", typeof(bool), ignoreCase),
            };
        }

        public void OnPluginLoaded(string hostName, string port, string proconVersion)
        {
            var className = typeof(PrivateMatchKicker).Name;
            RegisterEvents(className, "OnPlayerJoin", "OnListPlayers", "OnReservedSlotsPlayerAdded", "OnReservedSlotsPlayerRemoved", "OnReservedSlotsList", "OnReservedSlotsCleared");
        }

        public void OnPluginEnable()
        {
            reservedPlayers = null;

            ExecuteCommand("procon.protected.send", "reservedSlots.list");

            ExecuteCommand("procon.protected.pluginconsole.write", "^b" + GetPluginName() + " ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            reservedPlayers = null;

            ExecuteCommand("procon.protected.pluginconsole.write", "^b" + GetPluginName() + " ^1Disabled =(");
        }

        public void SetPluginVariable(string variable, string value)
        {
            switch (variable)
            {
                case "Case Insensitive Comparison":
                    if (bool.TryParse(value, out ignoreCase)) _UpdateIgnoreCase();
                    break;
            }
        }

        private void _UpdateIgnoreCase()
        {
            if (reservedPlayers != null && reservedPlayers.Comparer != _GetStringComparer())
            {
                reservedPlayers = new HashSet<string>(reservedPlayers, _GetStringComparer());
            }
        }

        public override void OnPlayerJoin(string soldierName)
        {
            if (reservedPlayers != null && !reservedPlayers.Contains(soldierName))
            {
                _KickPlayer(soldierName);
            }
        }

        public override void OnReservedSlotsPlayerAdded(string soldierName)
        {
            if (reservedPlayers == null)
            {
                reservedPlayers = new HashSet<string>(_GetStringComparer());
            }

            reservedPlayers.Add(soldierName);
        }

        public override void OnReservedSlotsPlayerRemoved(string soldierName)
        {
            if (reservedPlayers != null)
            {
                reservedPlayers.Remove(soldierName);

                _CheckAllPlayers();
            }
        }

        public override void OnReservedSlotsList(List<string> soldierNames)
        {
            if (soldierNames.Count > 0)
            {
                reservedPlayers = new HashSet<string>(soldierNames, _GetStringComparer());

                _CheckAllPlayers();
            }
            else
            {
                reservedPlayers = null;
            }
        }

        public override void OnReservedSlotsCleared()
        {
            reservedPlayers = null;
        }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            if (reservedPlayers == null)
            {
                return;
            }

            foreach (var player in players)
            {
                var soldierName = player.SoldierName;

                if (!reservedPlayers.Contains(soldierName))
                {
                    _KickPlayer(soldierName);
                }
            }
        }

        private void _CheckAllPlayers()
        {
            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        private StringComparer _GetStringComparer()
        {
            return ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }

        private void _KickPlayer(string soldierName)
        {
            ExecuteCommand("procon.protected.send", "admin.kickPlayer", soldierName, "Sorry for the kick, dude. This is a private match!");
            ExecuteCommand("procon.protected.send", "admin.say", "Kicked '" + soldierName + "' because this is a private match!");
        }
    }
}
