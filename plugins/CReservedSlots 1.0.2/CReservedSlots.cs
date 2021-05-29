/*##################################################################################################*
 *                                                                                                  *
 * Hello, thank you for downloading my plugin.                                                      *
 *                                                                                                  *
 * It will help you to always get on your server by using a 'fake' reserved slot system.            *
 * What this means is it will permamently reserve X slots on your server for VIPs.                  *
 *                                                                                                  *
 * When your server is not full you will not notice this, however, when your server is filling      *
 * you will find that your server will be almost always never be full. (62/64 | 46/48 etc)          *
 *                                                                                                  *
 * It does this by periodically checking the players list for VIP players and marking them as so,   *
 * whilst also often checking the server player count. If the server is full then it will           *
 * kick the last player to join that is not in the VIP list.                                        *
 *                                                                                                  *
 * The plugin will also reward those that populate your server with a reserved slot until the       *
 * server then empties.                                                                             *
 *                                                                                                  *
 *                                                                                                  *
 * Have fun always being able to get on your server without having to wait ages in the queue :)     *
 *                                  - Fruity-Tootie                                                 *
 *                                   http://Team-Aftershocks.com                                    *
 *                                                                                                  * 
 *#################################################################################################*/
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CReservedSlots : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables and Constructors
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        enumBoolYesNo m_enRefresh; // Refresh the plugin options
        enumBoolYesNo m_enReservedSlotsEnabled; // A soft enabler to stop and start kicking ingame
        private List<String> m_lstReservedSlots; // The list of reserved slots - will get some values from server list
        private List<String> m_lstPopulatorReward; // List of players that started the server and have been rewarded
        private int m_iServerSize; // How big the server is -- compatability with Adaptive Server Size
        private int m_iNumReserved; // How many slots the server should reserve
        private int m_iReservedSize; // How many random players can the server hold
        private int m_iPopulateRewardSize; // What playercount should populators stop being rewarded
        private string m_enKickReason; // The reason a player is kicked with

        private Dictionary<string, CPlayerJoinInf> dicPlayerCache = new Dictionary<string, CPlayerJoinInf>(); // Dictionary used to store all players
        private string m_strLastJoiner; // String to store the last joiner to enable an easier kick

        enumBoolYesNo m_enDebug; // Whether the plugin should print debug messages

        private bool m_isPluginEnabled;
        private bool boolplayerexists;
        private bool m_isKicking;

        public CReservedSlots()
        {

            this.m_enRefresh = enumBoolYesNo.No;
            this.m_enReservedSlotsEnabled = enumBoolYesNo.Yes;

            this.m_lstReservedSlots = new List<string>();
            this.m_lstReservedSlots.Add("Fruity-Tootie");

            this.m_enKickReason = "Automated Kick for reserved slot - sorry!";

            this.m_iNumReserved = 2;

            this.m_isKicking = false;

            this.m_lstPopulatorReward = new List<string>();

            this.dicPlayerCache = new Dictionary<string, CPlayerJoinInf>();
            this.boolplayerexists = false;

            this.m_enDebug = enumBoolYesNo.No;

            this.m_isPluginEnabled = false;
        }

        public string GetPluginName()
        {
            return "Reserved Slots";
        }

        public string GetPluginVersion()
        {
            return "1.0.2";
        }

        public string GetPluginAuthor()
        {
            return "Fruity";
        }

        public string GetPluginWebsite()
        {
            return "team-aftershocks.com";
        }

        public string GetPluginDescription()
        {
            return @"<h2>Description</h2>
			<p>This plugin allows VIPs to always connect to the server. It works like the reserved slots in Battlefield 2 by always having X number of slots free for VIP players.<br>
            Please consider donating if you find this useful to help you get onto your server. :)</p>

            <blockquote>
                <form action=""https://www.paypal.com/cgi-bin/webscr"" method=""post"" target=""_blank"">
				<input type=""hidden"" name=""cmd"" value=""_donations"">
				<input type=""hidden"" name=""business"" value=""3VRTPVGZP4ZWJ"">
				<input type=""hidden"" name=""lc"" value=""GB"">
				<input type=""hidden"" name=""item_name"" value=""Fruitys Plugins - Reserved Slots"">
				<input type=""hidden"" name=""currency_code"" value=""GBP"">
				<input type=""hidden"" name=""bn"" value=""PP-DonationsBF:btn_donate_LG.gif:NonHosted"">
				<input type=""image"" src=""https://www.paypalobjects.com/en_GB/i/btn/btn_donate_LG.gif"" border=""0"" name=""submit"" alt=""PayPal — The safer, easier way to pay online!"">
				</form>	
            </blockquote>

            <h2>Settings</h2>
                <blockquote><h4>Enabled?</h4>Whether the server will actively kick for a reserve slot to be in place. (Plugin will always remain enabled, but will not be active)</blockquote>
                <blockquote><h4>Refresh?</h4>Updates the list with the Reserved Slot list</blockquote>
                <blockquote><h4>Reserved Slots</h4>The <b>exact</b> player name of people you want to reserve a space for</blockquote>
                <blockquote><h4>Server size</h4>The maximum size your server can be (for compatibility with Adaptive server size)</blockquote>
                <blockquote><h4>Number of slots reserved</h4>How many slots the plugin should prevent from being connected to.</blockquote>
                <blockquote><h4>Populating Count</h4>Players that are on the server when it is below this amount will be rewarded with a reserved slot until the server empties. (Set to 0 to disable)</blockquote>
                <blockquote><h4>Kick Reason</h4>The message a player will recieved for being kicked.</blockquote>
                <blockquote><h4>Debug</h4>Whether the plugin should print what it is doing to the plugin console. (Will spam an incredible amount!)</blockquote>

            <h2>Ingame Commands</h2>
                <blockquote><h4>rson</h4>Turns the reserved slots on</blockquote>
                <blockquote><h4>rsoff</h4>Turns the reserved slots off</blockquote>

            <h2>Development</h2>
	            <h3>Changelog</h3>
                    <h4>1.0.2 - 10 May 2012</h4>
                    <ul><li>Fixed cycling last player issue.</li></ul>
                    
                    <h4>1.0.1 - 20 April 2012</h4>
                    <ul><li>Added validation to ensure kick command isn't repeated (fixes mass kicks)</li></ul>
                    <h4>1.0.0 - 14 March 2012 - <b>Initial Release</b></h4>
                    <ul><li>Completed main body of coding</li>
                        <li>Reserved slots implemented</li>
                        <li>Populator reward implemented</li></ul>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnReservedSlotsList");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bReserved Slot: ^2Enabled!");
            this.m_isPluginEnabled = true;
            RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bReserved Slot: ^1Disabled =(");
            this.m_isPluginEnabled = false;
            UnregisterAllCommands();
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option 
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Enabled?", typeof(enumBoolYesNo), this.m_enReservedSlotsEnabled));
            lstReturn.Add(new CPluginVariable("Refresh?", typeof(enumBoolYesNo), this.m_enRefresh));
            lstReturn.Add(new CPluginVariable("Reserved Slots", typeof(string[]), this.m_lstReservedSlots.ToArray()));
            lstReturn.Add(new CPluginVariable("Server size", typeof(int), this.m_iServerSize));
            lstReturn.Add(new CPluginVariable("Number of slots reserved", typeof(int), this.m_iNumReserved));
            lstReturn.Add(new CPluginVariable("Populating count", typeof(int), this.m_iPopulateRewardSize));
            lstReturn.Add(new CPluginVariable("Kick Reason", typeof(string), this.m_enKickReason));
            lstReturn.Add(new CPluginVariable("Debug", typeof(enumBoolYesNo), this.m_enDebug));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Enabled?", typeof(enumBoolYesNo), this.m_enReservedSlotsEnabled));
            lstReturn.Add(new CPluginVariable("Refresh?", typeof(enumBoolYesNo), this.m_enRefresh));
            lstReturn.Add(new CPluginVariable("Reserved Slots", typeof(string[]), this.m_lstReservedSlots.ToArray()));
            lstReturn.Add(new CPluginVariable("Server size", typeof(int), this.m_iServerSize));
            lstReturn.Add(new CPluginVariable("Number of slots reserved", typeof(int), this.m_iNumReserved));
            lstReturn.Add(new CPluginVariable("Populating count", typeof(int), this.m_iPopulateRewardSize));
            lstReturn.Add(new CPluginVariable("Kick Reason", typeof(string), this.m_enKickReason));
            lstReturn.Add(new CPluginVariable("Debug", typeof(enumBoolYesNo), this.m_enDebug));

            return lstReturn;
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iHelper;
            if (strVariable.CompareTo("Enabled?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enReservedSlotsEnabled = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Refresh?") == 0)
            {
                if (strValue == "Yes")
                {
                    this.ExecuteCommand("procon.protected.send", "reservedSlotsList.list");
                }
            }
            else if (strVariable.CompareTo("Reserved Slots") == 0)
            {
                this.m_lstReservedSlots = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Server size") == 0 && int.TryParse(strValue, out iHelper) == true)
            {
                this.m_iServerSize = iHelper;
            }
            else if (strVariable.CompareTo("Number of slots reserved") == 0 && int.TryParse(strValue, out iHelper) == true)
            {
                this.m_iNumReserved = iHelper;
            }
            else if (strVariable.CompareTo("Populating count") == 0 && int.TryParse(strValue, out iHelper) == true)
            {
                this.m_iPopulateRewardSize = iHelper;
            }
            else if (strVariable.CompareTo("Kick Reason") == 0)
            {
                this.m_enKickReason = strValue;
            }
            else if (strVariable.CompareTo("Debug") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDebug = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

        }

        private void UnregisterAllCommands()
        {
            this.UnregisterCommand(new MatchCommand("CReservedSlots", "OnCommandReservedEnable", this.Listify<string>("@", "!", "#"), "rson", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account), "Turns reserved slots on"));
            this.UnregisterCommand(new MatchCommand("CReservedSlots", "OnCommandReservedDisable", this.Listify<string>("@", "!", "#"), "rsoff", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account), "Turns reserved slots off"));
        }

        private void RegisterAllCommands()
        {

            if (this.m_isPluginEnabled == true)
            {
                this.RegisterCommand(new MatchCommand("CReservedSlots", "OnCommandReservedEnable", this.Listify<string>("@", "!", "#"), "rson", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account), "Turns reserved slots on"));
                this.RegisterCommand(new MatchCommand("CReservedSlots", "OnCommandReservedDisable", this.Listify<string>("@", "!", "#"), "rsoff", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account), "Turns reserved slots off"));
            }
        }

        private List<string> GetExcludedCommandStrings(string strAccountName)
        {

            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            CPrivileges privileges = this.GetAccountPrivileges(strAccountName);

            foreach (MatchCommand mtcCommand in lstCommands)
            {

                if (mtcCommand.Requirements.HasValidPermissions(privileges) == true && lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private List<string> GetCommandStrings()
        {

            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            foreach (MatchCommand mtcCommand in lstCommands)
            {

                if (lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        public void DebugInfo(string debugMessage)
        {
            if (m_enDebug == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bReserved Slots: ^n" + debugMessage);
            }
        }

        // Allows ingame admins to enable the plugin to start kicking again
        public void OnCommandReservedEnable(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.DebugInfo("Reserved slots soft enable");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Reserved slots ENABLED", "all");
            this.m_enReservedSlotsEnabled = enumBoolYesNo.Yes;
        }

        // Allows ingame admins to disable the plugin from kicking - but it will still keep track of last joiner!
        public void OnCommandReservedDisable(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.DebugInfo("Reserved slots soft disable");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Reserved slots DISABLED", "all");
            this.m_enReservedSlotsEnabled = enumBoolYesNo.No;
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                if (lstPlayers.Count < this.m_iPopulateRewardSize)
                {
                    foreach (CPlayerInfo cpiPlayer in lstPlayers)
                    {
                        if (!m_lstPopulatorReward.Contains(cpiPlayer.SoldierName))
                        {
                            this.m_lstPopulatorReward.Add(cpiPlayer.SoldierName);
                            this.DebugInfo(cpiPlayer.SoldierName + " added to the populator list");
                        }
                    }
                }

                if (lstPlayers.Count == 0)
                {
                    this.DebugInfo("Server is empty. Clearing cache dictionary");
                    this.dicPlayerCache.Clear(); //Clears player dictionary
                    this.m_lstPopulatorReward.Clear(); //Clears populator reward dictionary as the server is now dead
                }
                else
                {
                    this.DebugInfo("OnListPlayers - Server has " + lstPlayers.Count + " players");
                    foreach (KeyValuePair<string, CPlayerJoinInf> kvp in this.dicPlayerCache)
                    {
                        this.DebugInfo("OnListPlayers - Entered KeyValuePair foreach");
                        this.boolplayerexists = false;
                        foreach (CPlayerInfo cpiPlayer in lstPlayers)
                        {
                            this.DebugInfo("OnListPlayers - Entered CPlayerInfo foreach");
                            if (cpiPlayer.SoldierName == kvp.Key)
                            {
                                this.DebugInfo("OnListPlayers - Soldier (" + kvp.Key + ") is present, assigning values");
                                boolplayerexists = true;
                                if (this.dicPlayerCache[kvp.Key].teamID != cpiPlayer.TeamID)
                                {
                                    this.dicPlayerCache[kvp.Key].Playerjoined = DateTime.Now;
                                    this.dicPlayerCache[kvp.Key].playerWL = 0;
                                    this.dicPlayerCache[kvp.Key].teamID = cpiPlayer.TeamID;
                                }
                                this.dicPlayerCache[kvp.Key].playerSquad = cpiPlayer.SquadID;
                                this.DebugInfo("OnListPlayers - Checking if player is on reserved slots list");
                                if (((IList<string>)this.m_lstReservedSlots).Contains(cpiPlayer.SoldierName) || ((IList<string>)this.m_lstPopulatorReward).Contains(cpiPlayer.SoldierName))
                                {
                                    this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " is a VIP");
                                    this.dicPlayerCache[kvp.Key].playerWL = 1;
                                }
                                break;
                            }
                        }
                        if (boolplayerexists == false)
                        {
                            this.DebugInfo("OnListPlayers - Player does not exist");
                            this.dicPlayerCache.Remove(kvp.Key);
                        }
                    }

                    foreach (CPlayerInfo cpiPlayer in lstPlayers)
                    {
                        this.DebugInfo("OnListPlayers - Checking CPlayerInfo list");
                        if (this.dicPlayerCache.ContainsKey(cpiPlayer.SoldierName) == true)
                        {
                            this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " is in the cache");
                            if (this.dicPlayerCache[cpiPlayer.SoldierName].teamID != cpiPlayer.TeamID)
                            {
                                this.dicPlayerCache[cpiPlayer.SoldierName].teamID = cpiPlayer.TeamID;
                                this.DebugInfo("OnListPlayers - Updated team ID");
                            }
                            this.dicPlayerCache[cpiPlayer.SoldierName].playerSquad = cpiPlayer.SquadID;
                            this.DebugInfo("OnListPlayers - Checking if " + cpiPlayer.SoldierName + " is on Reserved List");
                            if (((IList<string>)this.m_lstReservedSlots).Contains(cpiPlayer.SoldierName) || ((IList<string>)this.m_lstPopulatorReward).Contains(cpiPlayer.SoldierName))
                            {
                                this.dicPlayerCache[cpiPlayer.SoldierName].playerWL = 1;
                                this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " is a VIP");
                            }
                            else
                            {
                                this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " is not a VIP");
                            }
                        }
                        else
                        {
                            this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " is not in the cache - adding now");
                            if (((IList<string>)this.m_lstReservedSlots).Contains(cpiPlayer.SoldierName))
                            {
                                CPlayerJoinInf newEntry = new CPlayerJoinInf(cpiPlayer.TeamID, 1, cpiPlayer.SquadID, DateTime.Now);
                                this.dicPlayerCache.Add(cpiPlayer.SoldierName, newEntry);
                                this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " added as a VIP");
                            }
                            else
                            {
                                CPlayerJoinInf newEntry = new CPlayerJoinInf(cpiPlayer.TeamID, 0, cpiPlayer.SquadID, DateTime.Now);
                                this.dicPlayerCache.Add(cpiPlayer.SoldierName, newEntry);
                                this.DebugInfo("OnListPlayers - " + cpiPlayer.SoldierName + " added as a player");
                            }
                        }
                    }
                }


                // 'Cycling players' bug fix starts ->
                this.m_iReservedSize = (this.m_iServerSize - this.m_iNumReserved);
                if (this.m_enReservedSlotsEnabled == enumBoolYesNo.Yes)
                {
                    if (dicPlayerCache.Count > this.m_iReservedSize)
                    {
                        this.DebugInfo("Kick Logic - Server is above Reserved Slot count. Checking kick status.");
                        if (this.m_isKicking == false)
                        {
                            this.m_isKicking = true;
                            this.DebugInfo("Kick Logic - Initialising kick process...");
                            KickLastJoiner();
                        }
                        else
                        {
                            this.DebugInfo("Kick Logic - Kick currently in progress");
                        }
                    }
                }
                // 'Cycling players' bug fix ends ^

            }
        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            if (this.dicPlayerCache.ContainsKey(cpiPlayer.SoldierName))
            {
                this.DebugInfo("Player left. Removing them from cache dictionary.");
                this.dicPlayerCache.Remove(cpiPlayer.SoldierName);
            }
        }

        public override void OnReservedSlotsList(List<string> soldierNames)
        {
            foreach (string name in soldierNames)
            {
                string reservedName = name.ToString();
                if (!m_lstReservedSlots.Contains(reservedName))
                {
                    this.m_lstReservedSlots.Add(reservedName.ToString());
                    this.DebugInfo(reservedName);
                }
            }
            this.GetDisplayPluginVariables();
        }

        public void KickLastJoiner()
        {
            this.m_isKicking = true;
            this.DebugInfo("KickLastJoiner - Checking if Kick should be carried out");
            if (this.m_enReservedSlotsEnabled == enumBoolYesNo.Yes)
            {
                DateTime maxValue = new DateTime();
                this.DebugInfo("KickLastJoiner - Finding who to kick now...");
                foreach (KeyValuePair<string, CPlayerJoinInf> kvp in dicPlayerCache)
                {
                    this.DebugInfo("KickLastJoiner - Inside KVP foreach. Checking WL != 1");
                    if (dicPlayerCache[kvp.Key].playerWL != 1)
                    {
                        this.DebugInfo("KickLastJoiner - " + kvp.Key + " is not a VIP, checking time joined.");
                        if (dicPlayerCache[kvp.Key].Playerjoined >= maxValue)
                        {
                            maxValue = dicPlayerCache[kvp.Key].Playerjoined;
                            this.m_strLastJoiner = kvp.Key;
                            this.DebugInfo("KickLastJoiner - Last joiner so far is " + kvp.Key);
                        }
                    }
                }
                this.DebugInfo("KickLastJoiner - The last joiner is: " + this.m_strLastJoiner + " - kicking now!");
                this.ExecuteCommand("procon.protected.send", "admin.say", "Kicking " + this.m_strLastJoiner + " for a reserved slot!", "all");
                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", this.m_strLastJoiner, this.m_enKickReason);
                if (this.m_enDebug == enumBoolYesNo.No)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^bReserved Slots:^n NOTICE: Kicking " + this.m_strLastJoiner + " for a reserved slot!");
                }
            }
            this.m_isKicking = false;
            this.ExecuteCommand("procon.protected.send", "admin.");
        }
    }

    #endregion

    #region Classes

    class CPlayerJoinInf
    {
        private int _teamID = 0;
        private int _playerWL = 0;
        private int _playerSquad = 0;
        private DateTime _Playerjoined;


        public int teamID
        {
            get { return _teamID; }
            set { _teamID = value; }
        }

        public int playerWL
        {
            get { return _playerWL; }
            set { _playerWL = value; }
        }

        public int playerSquad
        {
            get { return _playerSquad; }
            set { _playerSquad = value; }
        }

        public DateTime Playerjoined
        {
            get { return _Playerjoined; }
            set { _Playerjoined = value; }
        }

        public CPlayerJoinInf(int teamID, int playerWL, int playerSquad, DateTime Playerjoined)
        {
            _teamID = teamID;
            _playerWL = playerWL;
            _playerSquad = playerSquad;
            _Playerjoined = Playerjoined;
        }
    }
}
#endregion