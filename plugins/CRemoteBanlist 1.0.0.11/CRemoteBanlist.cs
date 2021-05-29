/*  Copyright 2011 Nick 'MorpheusX(AUT)' Mueller

    This file is part of MorpheusX(AUT)'s Plugins for Procon.

    MorpheusX(AUT)'s Plugins for Procon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MorpheusX(AUT)'s Plugins for Procon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MorpheusX(AUT)'s Plugins for Procon.  If not, see <http://www.gnu.org/licenses/>.

 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
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
    public class CRemoteBanlist : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region variables & constructor

        #region connection information

        /// <summary>
        /// hostname of the gameserver/procon-layer of the current connection
        /// </summary>
        private String strHostName;
        /// <summary>
        /// (rcon-)port of the gameserver/procon-layer of the current connection
        /// </summary>
        private String strPort;
        /// <summary>
        /// version of the current Procon-instance
        /// </summary>
        private String strPRoConVersion;

        #endregion

        #region dictionaries and lists

        /// <summary>
        /// dictionary of all current players and their CPlayerInfo-objects, using the playername as a key
        /// </summary>
        Dictionary<String, CPlayerInfo> dicCPlayerInfo;
        /// <summary>
        /// dictionary of all current players and their CPunkbusterInfo-objects, using the playername as a key
        /// </summary>
        Dictionary<String, CPunkbusterInfo> dicCPunkbusterInfo;
        /// <summary>
        /// list of all current players who have been checked already and are not banned
        /// </summary>
        List<String> lstCleanPlayers;
        /// <summary>
        /// list of all current CPlayerInfo-objects
        /// </summary>
        private List<CPlayerInfo> lstCPlayerInfo;
        /// <summary>
        /// CServerInfo-object for the current connection
        /// </summary>
        private CServerInfo csiServerInfo;
        /// <summary>
        /// current banlist stored on the gameserver
        /// </summary>
        /// 
        private List<CBanInfo> lstBanList;
        /// <summary>
        /// used to store the previous sent chat-message to prevent spam
        /// </summary>
        private String strPreviousMessage;
        /// <summary>
        /// used to store the previous speaker to prevent spam
        /// </summary>
        private String strPreviousSpeaker;
        /// <summary>
        /// used to store the command to check off length
        /// </summary>
        private string CheckBanCommand;
        /// <summary>
        /// used to store the command to check off length
        /// </summary>
        private int CheckBanCommandLen;
        #endregion

        #region SQL-details

        /// <summary>
        /// hostname of the SQL-server
        /// </summary>
        private String strSQLHost;
        /// <summary>
        /// port of the SQL-server
        /// </summary>
        private String strSQLPort;
        /// <summary>
        /// username to identify with the SQL-server
        /// </summary>
        private String strSQLUserName;
        /// <summary>
        /// password to identify with the SQL-server
        /// </summary>
        private String strSQLPassword;
        /// <summary>
        /// database to use
        /// </summary>
        private String strSQLDatabase;

        #endregion

        #region plugin settings

        /// <summary>
        /// keep the gameserver's banlist empty to avoid crashes
        /// </summary>
        private enumBoolYesNo ebEmptyServerBanlist;
        /// Dont kick player just announce findings for test
        /// </summary>

        // LEIBHOLD HACK ADDED
        private String chkstrMessage;
        private String NamesList;
        private String CheckSoldierName;
        private String CheckBanReturn;
        private enumBoolYesNo ebTestrun;
        private enumBoolYesNo ebEmptyServerPBBanlist;
        private enumBoolYesNo ebTestPBrun;
        private Dictionary<string, CPBBanInfo> PBBantest;
        private int BanCheckCommandLength;
        private string SQLmessage;
        private string cleanSQLmessage;
        /// <summary>
        /// toggles whether kicked players will be listed in the plugin-console
        /// </summary>
        private enumBoolYesNo ebPrintKicksToConsole;
        /// <summary>
        /// toggles whether basic debug-messages should be displayed
        /// </summary>
        private enumBoolYesNo ebBasicDebug;
        /// <summary>
        /// toggles whether full debug-messages should be displayed
        /// </summary>
        private enumBoolYesNo ebFullDebug;
        //debug level 1 = min 9 = verbose
        private int ebDebugLevel;
        /// <summary>
        /// ban-command used by the admin
        /// </summary>
        private string strBanCommand;
        /// <summary>
        /// send returned ban information to all players or just asker
        /// </summary
        private enumBoolYesNo SendBanToAll;
        /// <summary>
        /// send returned ban information to all players or just asker
        /// </summary
        private string SendBanToAllResult;

        #endregion

        #region others

        /// <summary>
        /// states whether the plugins is enabled or disabled
        /// </summary>
        private bool blPluginEnabled;

        #endregion

        public CRemoteBanlist()
        {
            this.dicCPlayerInfo = new Dictionary<String, CPlayerInfo>();
            this.dicCPunkbusterInfo = new Dictionary<String, CPunkbusterInfo>();
            this.lstCleanPlayers = new List<String>();
            this.lstCPlayerInfo = new List<CPlayerInfo>();
            this.lstBanList = new List<CBanInfo>();
            this.PBBantest = new Dictionary<string, CPBBanInfo>();
            this.strPreviousMessage = String.Empty;
            this.strPreviousSpeaker = String.Empty;

            this.strSQLHost = String.Empty;
            // String.Empty;
            this.strSQLPort = "3306";
            this.strSQLUserName = String.Empty;
            this.strSQLPassword = String.Empty;
            this.strSQLDatabase = String.Empty;

            this.ebEmptyServerBanlist = enumBoolYesNo.No;
            this.ebTestrun = enumBoolYesNo.Yes;
            this.ebEmptyServerPBBanlist = enumBoolYesNo.No;
            this.ebTestPBrun = enumBoolYesNo.Yes;
            this.ebPrintKicksToConsole = enumBoolYesNo.Yes;
            this.ebBasicDebug = enumBoolYesNo.No;
            this.ebFullDebug = enumBoolYesNo.No;
            this.ebDebugLevel = 0;
            this.strBanCommand = "bancheck";
            this.BanCheckCommandLength = 8;
            this.blPluginEnabled = false;
        }

        #endregion

        #region plugin details & settings

        public String GetPluginName()
        {
            return "Remote Banlist";
        }

        public String GetPluginVersion()
        {
            return "1.0.0.11";
        }

        public String GetPluginAuthor()
        {
            return "MorpheusX(AUT)";
        }

        public String GetPluginWebsite()
        {
            return "http://www.phogue.net/forumvb/member.php?565-MorpheusX(AUT)";
        }

        public String GetPluginDescription()
        {
            return @"<p align='center'>If you like my work, please consider donating!<br /><br />
            <form action='https://www.paypal.com/cgi-bin/webscr' method='post'>
            <input type='hidden' name='cmd' value='_s-xclick'>
            <input type='hidden' name='hosted_button_id' value='PLFJH26HK79AG'>
            <input type='image' src='https://www.paypal.com/en_US/i/btn/btn_donate_LG.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
            <img alt='' border='0' src='https://www.paypal.com/de_DE/i/scr/pixel.gif' width='1' height='1'>
            </form>
            <a href='https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=PLFJH26HK79AG'>Donation-Link</a></p>
            <h2>Description</h2>
            <p><b>Remote Banlist</b> is a plugin designed to simplify the work of managing large banlists across multiple servers.<br />
            Making use of a MySQL-Datebase, the plugin stores all its ban-information independent from the BF3-Server and it's banlists, also providing some more fields to add information like 'Banning Admin', 'Server Name', or some extra 'Comments'.<br />
            Bans can either be viewed or edited in the database directly, or in combination with leibhold's WebAdmin-Page, which also implements the RemoteBanlist-Plugin.<br />
            Please be aware: this is still an early release, so there might be some bugs within the code. Please report back if you encounter any errors!<br /></p>
            
            <h2>Settings</h2>
            <blockquote><h4>MySQL Host</h4>
			<p><i>Hostname or IP of your MySQL-Server</i></p>
			</blockquote>
            <blockquote><h4>MySQL Port</h4>
			<p><i>Port of your MySQL-Server</i></p>
			</blockquote>
            <blockquote><h4>MySQL Username</h4>
			<p><i>Username to identify with your MySQL-Server</i></p>
			</blockquote>
            <blockquote><h4>MySQL Password</h4>
			<p><i>Password to identify with your MySQL-Server</i></p>
			</blockquote>
            <blockquote><h4>MySQL Database</h4>
			<p><i>Name of the database to use</i></p>
			</blockquote>
            <blockquote><h4>Keep server-banlist empty?</h4>
			<p><i>If activated, the plugin issues a 'banList.clear' and 'banList.save' command after adding all bans in the list, thus keeping the local banlists clean. NOTE: <b>DO NOT USE WITH BANLISTS CONTAINING MORE THAN 100 ENTRIES!</b> (see 'Known bugs').</i></p>
			</blockquote>
            <blockquote><h4>Print kicked players to pluginconsole?</h4>
			<p><i>Prints a message to the pluginconsole, stating that a specific player has been found when kicking him.</i></p>
			</blockquote>
           <blockquote><h4>Debug levele</h4>
			<p><i>0 is no debug,  1 = min 9 = verbose messages to plugin-console</i></p>
			</blockquote>
            <blockquote><h4>Ingame-Bancommand (without @, #, !)</h4>
			<p><i>NOT FUNCTIONAL - The ingame-command you are using for banning people. NOT IN USE Enter this <b>without</b> any prefixes (like '@', '#', '!', '/', etc).</i></p>
			</blockquote>

            <h2>Known Bugs</h2>
            <blockquote><h4>'Keep server-banlist empty?' doesn't work properly with banlists > 100 entries</h4>
			<p><i><b>Remote Banlist</b> currently just uses the standard 'banList.list' command, which shows the first 100 bans. If there are more than that in the gameserver's banlist (NOT the remote banlist), the other ones will get lost. Working on a fix.</i></p>
			</blockquote>
            <blockquote><h4>PunkBuster-Bans don't get added to the remote banlist automatically</h4>
			<p><i>The automatic ban-add mechanism currently  supports adding PB-Bans to your MySQL-Database automatically.</i></p>
			</blockquote>";
        }

        public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
        {
            this.strHostName = strHostName;
            this.strPort = strPort;
            this.strPRoConVersion = strPRoConVersion;

            this.RegisterEvents(this.GetType().Name, "OnPlayerJoin", "OnPlayerLeft", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnListPlayers", "OnServerInfo", "OnLevelLoaded", "OnBanAdded", "OnBanRemoved", "OnBanList", "OnPunkbusterBanInfo", "OnPunkbusterUnbanInfo", "OnPunkbusterPlayerInfo");

        }

        public void OnPluginEnable()
        {
            this.dicCPlayerInfo.Clear();
            this.dicCPunkbusterInfo.Clear();
            this.lstCleanPlayers.Clear();
            this.lstCPlayerInfo.Clear();
            this.lstBanList.Clear();
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bRemote Banlist: ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "serverInfo");
            this.ExecuteCommand("procon.protected.send", "banList.list");
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            this.ExecuteCommand("procon.protected.tasks.add", "ProconPBlister", "1", "120", "-1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_banlist BC2!");
            this.blPluginEnabled = true;


        }

        public void OnPluginDisable()
        {
            this.dicCPlayerInfo.Clear();
            this.dicCPunkbusterInfo.Clear();
            this.lstCleanPlayers.Clear();
            this.lstCPlayerInfo.Clear();
            this.lstBanList.Clear();
            this.ExecuteCommand("procon.protected.tasks.remove", "ProconPBlister");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bRemote Banlist: ^1Disabled =(");

            this.blPluginEnabled = false;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Host", typeof(String), this.strSQLHost));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Port", typeof(String), this.strSQLPort));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Username", typeof(String), this.strSQLUserName));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Password", typeof(String), this.strSQLPassword));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Database", typeof(String), this.strSQLDatabase));

            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Keep server-banlist empty?", typeof(enumBoolYesNo), this.ebEmptyServerBanlist));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Test Run?", typeof(enumBoolYesNo), this.ebTestrun));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Keep server-PBbanlist empty?", typeof(enumBoolYesNo), this.ebEmptyServerPBBanlist));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Test PB Run?", typeof(enumBoolYesNo), this.ebTestPBrun));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Print kicked players to pluginconsole?", typeof(enumBoolYesNo), this.ebPrintKicksToConsole));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Debug Level?", this.ebDebugLevel.GetType(), this.ebDebugLevel));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Ingame-BanCheckcommand (without @, #, !)", typeof(string), this.strBanCommand));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Send Information Ban To All", typeof(enumBoolYesNo), this.SendBanToAll));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("MySQL Host", typeof(String), this.strSQLHost));
            lstReturn.Add(new CPluginVariable("MySQL Port", typeof(String), this.strSQLPort));
            lstReturn.Add(new CPluginVariable("MySQL Username", typeof(String), this.strSQLUserName));
            lstReturn.Add(new CPluginVariable("MySQL Password", typeof(String), this.strSQLPassword));
            lstReturn.Add(new CPluginVariable("MySQL Database", typeof(String), this.strSQLDatabase));

            lstReturn.Add(new CPluginVariable("Keep server-banlist empty?", typeof(enumBoolYesNo), this.ebEmptyServerBanlist));
            lstReturn.Add(new CPluginVariable("Test Run?", typeof(enumBoolYesNo), this.ebTestrun));
            lstReturn.Add(new CPluginVariable("Keep server-PBbanlist empty?", typeof(enumBoolYesNo), this.ebEmptyServerPBBanlist));
            lstReturn.Add(new CPluginVariable("Test PB Run?", typeof(enumBoolYesNo), this.ebTestPBrun));
            lstReturn.Add(new CPluginVariable("Print kicked players to pluginconsole?", typeof(enumBoolYesNo), this.ebPrintKicksToConsole));
            lstReturn.Add(new CPluginVariable("Debug Level?", this.ebDebugLevel.GetType(), this.ebDebugLevel));
            lstReturn.Add(new CPluginVariable("Ingame-BanCheckcommand (without @, #, !)", typeof(string), this.strBanCommand));
            lstReturn.Add(new CPluginVariable("Send Information Ban To All", typeof(enumBoolYesNo), this.SendBanToAll));

            return lstReturn;
        }

        public void SetPluginVariable(String strVariable, String strValue)
        {
            int iTmp;

            if (strVariable.CompareTo("MySQL Host") == 0)
            {
                this.strSQLHost = strValue;
            }
            else if (strVariable.CompareTo("MySQL Port") == 0)
            {
                if (int.TryParse(strValue, out iTmp) && (iTmp > 0 && iTmp <= 65535))
                {
                    this.strSQLPort = strValue;
                }
                else
                {
                    this.PluginConsoleWrite("Error while parsing MySQL-Port! You've probably not entered a valid number!");
                }
            }
            else if (strVariable.CompareTo("MySQL Username") == 0)
            {
                this.strSQLUserName = strValue;
            }
            else if (strVariable.CompareTo("MySQL Password") == 0)
            {
                this.strSQLPassword = strValue;
            }
            else if (strVariable.CompareTo("MySQL Database") == 0)
            {
                this.strSQLDatabase = strValue;
            }
            else if (strVariable.CompareTo("Keep server-banlist empty?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebEmptyServerBanlist = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Test Run?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebTestrun = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Keep server-PBbanlist empty?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebEmptyServerPBBanlist = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Test PB Run?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebTestPBrun = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Print kicked players to pluginconsole?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebPrintKicksToConsole = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Debug Level?") == 0 && int.TryParse(strValue, out iTmp) == true)
            {
                this.ebDebugLevel = iTmp;
            }
            else if (strVariable.CompareTo("Ingame-BanCheckcommand (without @, #, !)") == 0)
            {
                this.strBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Send Information Ban To All") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.SendBanToAll = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
        }

        private void UnregisterAllCommands()
        {

        }

        private void SetupHelpCommands()
        {

        }

        private void RegisterAllCommands()
        {


        }

        #endregion

        #region events

        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.csiServerInfo = csiServerInfo;
        }







        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset cpsSubset)
        {
            this.ScaledDebugInfo(1, "OnListPlayers fired");
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                this.lstCPlayerInfo = players;

                foreach (CPlayerInfo player in this.lstCPlayerInfo)
                {
                    // update or add the PlayerInformation about a player
                    lock (this.dicCPlayerInfo)
                    {
                        if (this.dicCPlayerInfo.ContainsKey(player.SoldierName))
                        {
                            this.dicCPlayerInfo[player.SoldierName] = player;
                        }
                        else
                        {
                            this.dicCPlayerInfo.Add(player.SoldierName, player);
                        }
                    }

                    // use a boolean as a trigger so the lock around lstCleanPlayers doesn't lead to a deadlock when calling CheckPlayerBanned
                    bool blChecked = false;

                    // see if a player has been checked already, skipping him if he's "clean"
                    lock (this.lstCleanPlayers)
                    {
                        blChecked = this.lstCleanPlayers.Contains(player.SoldierName);
                    }

                    if (!blChecked)
                    {
                        this.CheckPlayerBanned(player.SoldierName);
                    }
                }
            }
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo playerInfo)
        {
            this.ScaledDebugInfo(1, "OnPunkbusterPlayerInfo fired");
            // update or add the PunkbusterInformation about a player
            lock (this.dicCPunkbusterInfo)
            {
                if (this.dicCPunkbusterInfo.ContainsKey(playerInfo.SoldierName))
                {
                    this.dicCPunkbusterInfo[playerInfo.SoldierName] = playerInfo;
                }
                else
                {
                    this.dicCPunkbusterInfo.Add(playerInfo.SoldierName, playerInfo);
                }
            }
        }

        public override void OnPlayerJoin(String soldierName)
        {

        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            this.ScaledDebugInfo(1, "OnPlayerLeft fired");
            // remove all information stored about a player
            // this might lead to extra work when a player disconnected/reconnects, but saves ressources
            lock (this.dicCPlayerInfo)
            {
                if (this.dicCPlayerInfo.ContainsKey(playerInfo.SoldierName))
                {
                    this.dicCPlayerInfo.Remove(playerInfo.SoldierName);
                }
            }

            lock (this.dicCPunkbusterInfo)
            {
                if (this.dicCPunkbusterInfo.ContainsKey(playerInfo.SoldierName))
                {
                    this.dicCPunkbusterInfo.Remove(playerInfo.SoldierName);
                }
            }

            lock (this.lstCleanPlayers)
            {
                if (this.lstCleanPlayers.Contains(playerInfo.SoldierName))
                {
                    this.lstCleanPlayers.Remove(playerInfo.SoldierName);
                }
            }
        }

        public override void OnGlobalChat(string strSpeaker, string strMessage)
        {
            this.ScaledDebugInfo(1, "OnGlobalChat fired");
            // just filter all messages containing the bancommand and not being send by the server





            if (strMessage.StartsWith(this.strBanCommand) && strSpeaker.CompareTo("Server") != 0)
            {
                CheckBanCommand = this.strBanCommand;
                CheckBanCommandLen = this.strBanCommand.Length + 1;


                if (strMessage.Length > CheckBanCommand.Length)
                {
                    if (this.SendBanToAll == enumBoolYesNo.Yes)
                    {
                        SendBanToAllResult = "all";
                    }
                    else
                    {
                        SendBanToAllResult = strSpeaker;
                    }

                    this.ScaledDebugInfo(3, "Who to Say- " + SendBanToAllResult + "- completed");

                    // only do stuff if the messages are not identical
                    if (this.strPreviousMessage.CompareTo(strMessage) != 0)
                    {

                        this.ScaledDebugInfo(2, "Found Checkbancommand in message '" + strMessage + "'. Calling banList.list...");
                        this.ScaledDebugInfo(3, "Checking Player - " + strMessage.Substring(CheckBanCommandLen, strMessage.Length - CheckBanCommandLen));

                        CheckBanReturn = CheckBanbyName(strMessage.Substring(CheckBanCommandLen, strMessage.Length - CheckBanCommandLen));
                        this.ScaledDebugInfo(2, "Return was  '" + CheckBanReturn + "'  ");
                        IngameSayTo(CheckBanReturn, SendBanToAllResult);

                    }
                    else if (this.strPreviousMessage.CompareTo(strMessage) == 0 && this.strPreviousSpeaker.CompareTo(strSpeaker) != 0)
                    {


                        this.ScaledDebugInfo(2, "Found Checkbancommand in message '" + strMessage + "'. Calling banList.list...");
                        this.ScaledDebugInfo(3, "CHECK" + strMessage.Substring(CheckBanCommandLen, strMessage.Length - CheckBanCommandLen));

                        CheckBanReturn = CheckBanbyName(strMessage.Substring(CheckBanCommandLen, strMessage.Length - CheckBanCommandLen));
                        this.ScaledDebugInfo(2, "Return was  '" + CheckBanReturn + "'  ");
                        IngameSayTo(CheckBanReturn, SendBanToAllResult);
                    }
                    else
                    {

                        this.ScaledDebugInfo(2, "Identical chatmessage + speaker. Ignoring");
                    }



                } // end of no player name check


            }
        }

        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            // doesn't matter what kind of chat the message was typed
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public override void OnBanAdded(CBanInfo ban)
        {
            this.ScaledDebugInfo(1, "OnBanAdded fired");
            this.ScaledDebugInfo(2, "OnBanAdded (Name: " + ban.SoldierName + ", GUID: " + ban.Guid + ", IP: " + ban.IpAddress + ", IdType: " + ban.IdType + ", BanLength: " + ban.BanLength.Subset + ", BanSeconds: " + ban.BanLength.Seconds + ", Reason: " + ban.Reason + ")...");

            if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent || (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds && ban.BanLength.Seconds >= 43200))
            {
                this.ScaledDebugInfo(3, "OnBanAdded This is a perm ban - passed the check");
                CRemoteBanInfo rBan = new CRemoteBanInfo();
                rBan.SoldierName = ban.SoldierName;
                rBan.Reason = ban.Reason;
                rBan.Reason = SQLcleanup(rBan.Reason);

                if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent)
                {
                    rBan.Length = BanLength.Permanent;
                }
                else if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds)
                {
                    rBan.Length = BanLength.Seconds;
                    rBan.Duration = ban.BanLength.Seconds.ToString();
                }

                switch (ban.IdType.ToLower())
                {
                    case "name":
                        rBan.Type = BanType.Name;
                        break;
                    case "guid":
                        rBan.Type = BanType.EAGUID;
                        break;
                    case "pb guid":
                        rBan.Type = BanType.PBGUID;
                        break;
                    case "ip":
                        rBan.Type = BanType.IP;
                        break;
                    default:
                        rBan.Type = BanType.Name;
                        break;
                }

                if (rBan.Type == BanType.Name && (this.dicCPlayerInfo.ContainsKey(ban.SoldierName) && this.dicCPunkbusterInfo.ContainsKey(ban.SoldierName)))
                {
                    lock (this.dicCPlayerInfo)
                    {
                        rBan.EAGUID = this.dicCPlayerInfo[ban.SoldierName].GUID;
                    }
                    lock (this.dicCPunkbusterInfo)
                    {
                        rBan.PBGUID = this.dicCPunkbusterInfo[ban.SoldierName].GUID;
                        if (!String.IsNullOrEmpty(this.dicCPunkbusterInfo[ban.SoldierName].Ip) && this.dicCPunkbusterInfo[ban.SoldierName].Ip.CompareTo("") != 0 && this.dicCPunkbusterInfo[ban.SoldierName].Ip.Contains(":"))
                        {
                            String[] ipPort = this.dicCPunkbusterInfo[ban.SoldierName].Ip.Split(':');
                            rBan.IP = ipPort[0];
                        }
                        else
                        {
                            rBan.IP = this.dicCPunkbusterInfo[ban.SoldierName].Ip;
                        }
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(ban.IpAddress) && ban.IpAddress.CompareTo("") != 0 && ban.IpAddress.Contains(":"))
                    {
                        String[] ipPort = ban.IpAddress.Split(':');
                        rBan.IP = ipPort[0];
                    }
                    else
                    {
                        rBan.IP = ban.IpAddress;
                    }
                    if (rBan.Type == BanType.PBGUID)
                    {
                        rBan.PBGUID = ban.Guid;
                    }
                    else
                    {
                        rBan.EAGUID = ban.Guid;
                    }
                }

                this.AddBan(rBan);
                this.ExecuteCommand("procon.protected.send", "banList.list");
            }

            this.ScaledDebugInfo(1, "Leaving OnBanAdded (Name: " + ban.SoldierName + ", GUID: " + ban.Guid + ", IP: " + ban.IpAddress + ", IdType: " + ban.IdType + ", BanLength: " + ban.BanLength.Subset + ", BanSeconds: " + ban.BanLength.Seconds + ", Reason: " + ban.Reason + ")...");
        }

        public override void OnBanRemoved(CBanInfo ban)
        {
            this.ScaledDebugInfo(1, "OnBanRemoved Fired");

            if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent || (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds && ban.BanLength.Seconds >= 43200))
            {
                CRemoteBanInfo rBan = new CRemoteBanInfo();
                rBan.SoldierName = ban.SoldierName;
                rBan.Reason = ban.Reason;

                if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent)
                {
                    rBan.Length = BanLength.Permanent;
                }
                else if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds)
                {
                    rBan.Length = BanLength.Seconds;
                    rBan.Duration = ban.BanLength.Seconds.ToString();
                }

                if (this.dicCPlayerInfo.ContainsKey(ban.SoldierName) && this.dicCPunkbusterInfo.ContainsKey(ban.SoldierName))
                {
                    lock (this.dicCPlayerInfo)
                    {
                        rBan.EAGUID = this.dicCPlayerInfo[ban.SoldierName].GUID;
                    }
                    lock (this.dicCPunkbusterInfo)
                    {
                        rBan.PBGUID = this.dicCPunkbusterInfo[ban.SoldierName].GUID;
                        if (!String.IsNullOrEmpty(this.dicCPunkbusterInfo[ban.SoldierName].Ip) && this.dicCPunkbusterInfo[ban.SoldierName].Ip.CompareTo("") != 0 && this.dicCPunkbusterInfo[ban.SoldierName].Ip.Contains(":"))
                        {
                            String[] ipPort = this.dicCPunkbusterInfo[ban.SoldierName].Ip.Split(':');
                            rBan.IP = ipPort[0];
                        }
                        else
                        {
                            rBan.IP = this.dicCPunkbusterInfo[ban.SoldierName].Ip;
                        }
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(ban.IpAddress) && ban.IpAddress.CompareTo("") != 0 && ban.IpAddress.Contains(":"))
                    {
                        String[] ipPort = ban.IpAddress.Split(':');
                        rBan.IP = ipPort[0];
                    }
                    else
                    {
                        rBan.IP = ban.IpAddress;
                    }
                    rBan.EAGUID = ban.Guid;
                }

                this.RemoveBan(rBan);
            }
        }

        public override void OnBanList(List<CBanInfo> banList)
        {
            this.ScaledDebugInfo(1, "OnBanList Fired");
            this.lstBanList.Clear();
            this.lstBanList = banList;

            this.ScaledDebugInfo(2, "Banlist with " + lstBanList.Count + " entries...");


            foreach (CBanInfo ban in this.lstBanList)
            {
                this.ScaledDebugInfo(3, "Entering OnBanList (Name: " + ban.SoldierName + ", GUID: " + ban.Guid + ", IP: " + ban.IpAddress + ", IdType: " + ban.IdType + ", BanLength: " + ban.BanLength.Subset + ", BanSeconds: " + ban.BanLength.Seconds + ", Reason: " + ban.Reason + ")...");

                // only store the ban if it's permanent or longer than 12 hours
                if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent || (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds && ban.BanLength.Seconds >= 43200))
                {
                    CRemoteBanInfo rBan = new CRemoteBanInfo();
                    rBan.SoldierName = ban.SoldierName;
                    rBan.Reason = ban.Reason;

                    if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent)
                    {
                        rBan.Length = BanLength.Permanent;
                    }
                    else if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds)
                    {
                        rBan.Length = BanLength.Seconds;
                        rBan.Duration = ban.BanLength.Seconds.ToString();
                    }

                    switch (ban.IdType.ToLower())
                    {
                        case "name":
                            rBan.Type = BanType.Name;
                            break;
                        case "guid":
                            rBan.Type = BanType.EAGUID;
                            break;
                        case "pb guid":
                            rBan.Type = BanType.PBGUID;
                            break;
                        case "ip":
                            rBan.Type = BanType.IP;
                            break;
                        default:
                            rBan.Type = BanType.Name;
                            break;
                    }

                    if (rBan.Type == BanType.Name && (this.dicCPlayerInfo.ContainsKey(ban.SoldierName) && this.dicCPunkbusterInfo.ContainsKey(ban.SoldierName)))
                    {
                        lock (this.dicCPlayerInfo)
                        {
                            rBan.EAGUID = this.dicCPlayerInfo[ban.SoldierName].GUID;
                        }
                        lock (this.dicCPunkbusterInfo)
                        {
                            rBan.PBGUID = this.dicCPunkbusterInfo[ban.SoldierName].GUID;
                            if (!String.IsNullOrEmpty(this.dicCPunkbusterInfo[ban.SoldierName].Ip) && this.dicCPunkbusterInfo[ban.SoldierName].Ip.CompareTo("") != 0 && this.dicCPunkbusterInfo[ban.SoldierName].Ip.Contains(":"))
                            {
                                String[] ipPort = this.dicCPunkbusterInfo[ban.SoldierName].Ip.Split(':');
                                rBan.IP = ipPort[0];
                            }
                            else
                            {
                                rBan.IP = this.dicCPunkbusterInfo[ban.SoldierName].Ip;
                            }
                        }
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(ban.IpAddress) && ban.IpAddress.CompareTo("") != 0 && ban.IpAddress.Contains(":"))
                        {
                            String[] ipPort = ban.IpAddress.Split(':');
                            rBan.IP = ipPort[0];
                        }
                        else
                        {
                            rBan.IP = ban.IpAddress;
                        }
                        if (rBan.Type == BanType.PBGUID)
                        {
                            rBan.PBGUID = ban.Guid;
                        }
                        else
                        {
                            rBan.EAGUID = ban.Guid;
                        }
                    }


                    this.ScaledDebugInfo(6, "Ban reason Cleanup - original " + rBan.Reason);
                    rBan.Reason = SQLcleanup(rBan.Reason);
                    this.ScaledDebugInfo(6, "Ban reason Cleanup - New      " + rBan.Reason);


                    this.AddBan(rBan);

                    // LEIBHOLD HACK ADDED
                    this.ScaledDebugInfo(3, "Ban Added - now check");

                    if (this.CheckBan(rBan))
                    {
                        this.ScaledDebugInfo(3, "Checkban shows good");
                        //So now we remove the ban from the server

                        if (this.ebTestrun == enumBoolYesNo.Yes)
                        {
                            switch (ban.IdType.ToLower())
                            {
                                case "name":
                                    this.ScaledDebugInfo(4, "banList.remove type " + ban.IdType + " name " + rBan.SoldierName);
                                    break;
                                case "guid":
                                    this.ScaledDebugInfo(4, "banList.remove type " + ban.IdType + " name " + rBan.EAGUID);
                                    break;
                                case "pb guid":
                                    this.ScaledDebugInfo(4, "banList.remove type " + ban.IdType + " name " + rBan.PBGUID);
                                    break;
                                case "ip":
                                    this.ScaledDebugInfo(4, "banList.remove type " + ban.IdType + " info " + rBan.IP);
                                    break;
                                default:
                                    this.ScaledDebugInfo(4, "DEAFULTED banList.remove type " + ban.IdType + " info " + rBan.SoldierName);
                                    break;
                            }
                        }
                        else
                        {
                            if (this.ebEmptyServerBanlist == enumBoolYesNo.Yes)
                            {

                                switch (ban.IdType.ToLower())
                                {
                                    case "name":
                                        this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", ban.IdType, rBan.SoldierName);
                                        this.ScaledDebugInfo(4, "REM ban shows name --------------- done");
                                        break;
                                    case "guid":
                                        this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", ban.IdType, rBan.EAGUID);
                                        this.ScaledDebugInfo(4, "REM ban shows GUID --------------- done");
                                        break;
                                    case "pb guid":
                                        this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "punkBuster.pb_sv_command pb_sv_unbanguid", ban.IdType, rBan.PBGUID);
                                        this.ScaledDebugInfo(4, "REMban shows pbguid --------------- done");
                                        break;
                                    case "ip":
                                        this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", ban.IdType, rBan.IP);
                                        this.ScaledDebugInfo(4, "REM ban shows  IP --------------- done");
                                        break;
                                    default:
                                        this.ScaledDebugInfo(4, "REM ban shows NAME DEFAULT --------------- done");
                                        break;
                                }
                            }
                        }

                    }
                    else
                    {
                        this.ScaledDebugInfo(3, "Checkban shows ban wasnt added to the database to leave it alone");
                    }

                }

                this.ScaledDebugInfo(1, "Leaving OnBanList (Name: " + ban.SoldierName + ", GUID: " + ban.Guid + ", IP: " + ban.IpAddress + ", IdType: " + ban.IdType + ", BanLength: " + ban.BanLength.Subset + ", BanSeconds: " + ban.BanLength.Seconds + ", Reason: " + ban.Reason + ")...");
            }

        }

        public override void OnLevelLoaded(String mapFileName, String Gamemode, int roundsPlayed, int roundsTotal)
        {

        }


        public override void OnPunkbusterBanInfo(CBanInfo ban)
        {

            this.ScaledDebugInfo(1, "Entering PBBans Info (Name: " + ban.SoldierName + ", GUID: " + ban.Guid + ", IP: " + ban.IpAddress + ", IdType: " + ban.IdType + ", BanLength: " + ban.BanLength.Subset + ", BanSeconds: " + ban.BanLength.Seconds + ", Reason: " + ban.Reason + ")...");

            // Only do the ban if its got a BC2 in the reason start
            this.ScaledDebugInfo(2, "Checking for BC2 Ban");
            if (ban.Reason.Substring(0, 3) == "BC2")
            {
                this.ScaledDebugInfo(3, "Test for BC2 PASSED");
                // only store the ban if it's permanent
                this.ScaledDebugInfo(2, "Checking if PERM ban");
                if (ban.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Permanent)
                {
                    this.ScaledDebugInfo(3, "Test for Perm Ban - PASSED");

                    // start Checking to see if we have seen this ban before.
                    if (!this.PBBantest.ContainsKey(ban.Guid))
                    {
                        this.ScaledDebugInfo(4, "New Ban on the list - adding");
                        //guid isnt on the list so create a new check ban entry
                        this.PBBantest.Add(ban.Guid, new CPBBanInfo(ban.Guid));
                        this.PBBantest[ban.Guid].SoldierName = ban.SoldierName;

                    }
                    else
                    {
                        this.ScaledDebugInfo(4, "Seen before ban - checking times and counts");
                        //found the guid - start comparing values to see if we add this ban to the database
                        // Add one to the see count
                        this.PBBantest[ban.Guid].BanCount++;
                        //get how long its been
                        TimeSpan tsTimeSinceLastRequest = DateTime.Now.Subtract(this.PBBantest[ban.Guid].TimeStamp);
                        this.ScaledDebugInfo(5, "Its been " + tsTimeSinceLastRequest.TotalSeconds + " seconds since we first saw this ban and " + this.PBBantest[ban.Guid].BanCount + " times");
                        if (tsTimeSinceLastRequest.TotalSeconds >= 180)
                        {
                            this.ScaledDebugInfo(4, "Its been more than 180 seconds so process it ");
                            // we would then process the ban
                            CRemoteBanInfo rBan = new CRemoteBanInfo();
                            rBan.SoldierName = ban.SoldierName;
                            // Get rid of the BC2! and swap it to PBan
                            rBan.Reason = ban.Reason.Substring(4, (ban.Reason.Length - 4));
                            // clean it up incase there are funny characters like ' /\
                            rBan.Reason = SQLcleanup(rBan.Reason);
                            rBan.Reason = "PBAN " + rBan.Reason;
                            rBan.Length = BanLength.Permanent;
                            rBan.Type = BanType.PBGUID;

                            this.ScaledDebugInfo(4, "Ban type = " + rBan.Type + " and type is " + ban.IdType);

                            // add hack for ??? ip address from manual ban


                            if (!String.IsNullOrEmpty(ban.IpAddress) && ban.IpAddress.CompareTo("") != 0 && ban.IpAddress.Contains(":"))
                            {
                                String[] ipPort = ban.IpAddress.Split(':');
                                rBan.IP = ipPort[0];
                            }
                            else
                            {
                                rBan.IP = ban.IpAddress;
                            }


                            if (rBan.IP == "???")
                            {
                                rBan.IP = null;
                            }


                            if (rBan.Type == BanType.PBGUID)
                            {
                                rBan.PBGUID = ban.Guid;
                            }
                            else
                            {
                                rBan.EAGUID = ban.Guid;
                            }

                            this.ScaledDebugInfo(4, "Adding Ban to database Name: " + rBan.SoldierName + ", GUID: " + rBan.PBGUID + ", IP: " + rBan.IP + " Reason: " + rBan.Reason);
                            this.AddBan(rBan);
                            this.ScaledDebugInfo(3, "Ban Added - now check");

                            if (this.CheckBan(rBan))
                            {
                                this.ScaledDebugInfo(4, "Checkban shows good");
                                //So now we remove the ban from the server

                                if (this.ebTestPBrun == enumBoolYesNo.Yes)
                                {
                                    this.ScaledDebugInfo(4, "PbBan would have been removed for PBGUID " + rBan.PBGUID);
                                    this.PBBantest.Remove(ban.Guid);
                                    this.ScaledDebugInfo(4, "Remove PB Ban  from check list");
                                }
                                else
                                {
                                    if (this.ebEmptyServerPBBanlist == enumBoolYesNo.Yes)
                                    {
                                        this.ScaledDebugInfo(4, "Empty Server List if isnt a test and empty server list is on");
                                        this.ExecuteCommand("procon.protected.tasks.add", "RemovePBBan", "0", "1", "1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_unbanguid " + rBan.PBGUID);
                                        this.ScaledDebugInfo(4, "Remove PB Ban  completed");
                                        this.PBBantest.Remove(ban.Guid);
                                        this.ScaledDebugInfo(4, "Remove PB Ban  from check list");
                                    }
                                }

                            }
                            else
                            {
                                this.ScaledDebugInfo(4, "Checkban shows ban wasnt added to the database for some reason so dont delete it");
                            }


                        }

                    }


                    this.ScaledDebugInfo(3, "Check Ban Completed");
                }
                this.ScaledDebugInfo(2, "Ban and Check Ban Completed");
            }
            this.ScaledDebugInfo(1, "Leaving pbBanList ");

        }

        public override void OnPunkbusterUnbanInfo(CBanInfo unban)
        {

        }





        #endregion

        #region other methods

        /// <summary>
        /// execute a ban
        /// </summary>
        /// <param name="name">name of the player to remove</param>
        /// <param name="reason">reason shown to the player</param>
        private void TakeAction(String name, String reason)
        {
            if (this.ebTestrun == enumBoolYesNo.Yes)
            {
                this.DebugInfo("test", "  name" + name + " would have been kicked for " + reason);
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", name, reason);
            }
        }


        /// <summary>
        /// Check PB bans - we have to fire off our own check PB to list the bans
        /// </summary>
        private void CheckPB(String locked, String reason)
        {
            this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_banlist BC2!");
        }


        /// <summary>
        /// check's whether the remotebanlist-table already exists, creating it if not
        /// </summary>
        private void CheckDatabase()
        {
            OdbcConnection OdbcCon = null;

            try
            {
                OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                       "SERVER=" + this.strSQLHost + ";" +
                                                                       "PORT=" + this.strSQLPort + ";" +
                                                                       "DATABASE=" + this.strSQLDatabase + ";" +
                                                                       "UID=" + this.strSQLUserName + ";" +
                                                                       "PWD=" + this.strSQLPassword + ";" +
                                                                       "OPTION=3;");

                OdbcCon.Open();

                if (OdbcCon.State == ConnectionState.Open)
                {
                    string sql = @"CREATE TABLE IF NOT EXISTS `" + this.strSQLDatabase + @"`.`remotebanlist` (
                                    `id` INT( 11 ) NOT NULL AUTO_INCREMENT,
                                    `ClanTag` VARCHAR( 10 ) DEFAULT NULL,
                                    `SoldierName` VARCHAR( 50 ) DEFAULT NULL,
                                    `EAGUID` VARCHAR( 35 ) DEFAULT NULL,
                                    `PBGUID` VARCHAR( 32 ) DEFAULT NULL,
                                    `ÍP_Address` VARCHAR( 15 ) DEFAULT NULL,
                                    `reason` VARCHAR( 150 ) NOT NULL DEFAULT '-Banned using RemoteBanlist-',
                                    `comment` TEXT,
                                    `banning_admin` VARCHAR( 50 ) NOT NULL DEFAULT '-Unknown-',
                                    `bantype` ENUM( 'perm', 'seconds' ) NOT NULL DEFAULT 'perm',
                                    `banduration` VARCHAR( 50 ) DEFAULT NULL,
                                    `expired` VARCHAR( 1 ) NOT NULL DEFAULT 'n',
                                    `servername` VARCHAR( 150 ) DEFAULT NULL,
                                    `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";

                    using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                    {
                        OdbcCom.ExecuteNonQuery();
                    }
                }
                else
                {
                    this.PluginConsoleWrite("OdbcConnection could not be opened at CheckDatabase!");
                }
            }
            catch (Exception e)
            {
                this.PluginConsoleWrite("Exception while CheckDatabase: " + e.ToString());
            }
            finally
            {
                OdbcCon.Close();
            }
        }

        /// <summary>
        /// checks whether a player is banned using RemoteBanlist
        /// </summary>
        /// <param name="name">name of the player</param>
        private void CheckPlayerBanned(String name)
        {
            // check if the database exists, create it if not
            if (this.strSQLHost != String.Empty && this.strSQLPort != String.Empty && this.strSQLDatabase != String.Empty && this.strSQLUserName != String.Empty && this.strSQLPassword != String.Empty)
            {
                this.CheckDatabase();
            }
            else
            {
                this.PluginConsoleWrite("Please enter all database-details!");
                return;
            }

            OdbcConnection OdbcCon = null;

            try
            {
                OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                       "SERVER=" + this.strSQLHost + ";" +
                                                                       "PORT=" + this.strSQLPort + ";" +
                                                                       "DATABASE=" + this.strSQLDatabase + ";" +
                                                                       "UID=" + this.strSQLUserName + ";" +
                                                                       "PWD=" + this.strSQLPassword + ";" +
                                                                       "OPTION=3;");

                OdbcCon.Open();

                if (OdbcCon.State == ConnectionState.Open)
                {
                    CPlayerInfo player = null;
                    CPunkbusterInfo pplayer = null;

                    lock (this.dicCPlayerInfo)
                    {
                        if (this.dicCPlayerInfo.ContainsKey(name))
                        {
                            player = this.dicCPlayerInfo[name];
                        }
                        else
                        {
                            return;
                        }
                    }

                    lock (this.dicCPunkbusterInfo)
                    {
                        if (this.dicCPunkbusterInfo.ContainsKey(name))
                        {
                            pplayer = this.dicCPunkbusterInfo[name];
                        }
                        else
                        {
                            return;
                        }
                    }

                    String ip;
                    if (!String.IsNullOrEmpty(pplayer.Ip) && pplayer.Ip.CompareTo("") != 0 && pplayer.Ip.Contains(":"))
                    {
                        String[] ipPort = pplayer.Ip.Split(':');
                        ip = ipPort[0];
                    }
                    else
                    {
                        ip = pplayer.Ip;
                    }

                    //String sql = @"SELECT * FROM `" + this.strSQLDatabase + @"`.`remotebanlist` WHERE (SoldierName LIKE '" + name + "' OR EAGUID LIKE '" + player.EAGUID + "' OR PBGUID LIKE '" + pplayer.GUID + "' OR IP_Address LIKE '" + ip + "') AND expired NOT LIKE 'y';";

                    String sql = @"SELECT * FROM `" + this.strSQLDatabase + @"`.`remotebanlist` ";
                    bool sqlender = true;

                    if (!String.IsNullOrEmpty(name))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        sql += "SoldierName LIKE '" + name + "'";
                    }

                    if (!String.IsNullOrEmpty(pplayer.GUID))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " PBGUID LIKE '" + pplayer.GUID + "'";
                    }


                    if (!String.IsNullOrEmpty(player.GUID))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " EAGUID LIKE '" + player.GUID + "'";
                    }
                    if (!String.IsNullOrEmpty(ip))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " IP_Address LIKE '" + ip + "'";
                    }
                    sql += ") AND expired NOT LIKE 'y';";

                    this.DebugInfo("basic", sql);

                    using (OdbcCommand OdbcCommand = new OdbcCommand(sql, OdbcCon))
                    {
                        DataTable dtData = new DataTable();

                        using (OdbcDataAdapter OdbcAdapter = new OdbcDataAdapter(OdbcCommand))
                        {
                            OdbcAdapter.Fill(dtData);
                        }

                        if (dtData.Rows != null && dtData.Rows.Count > 0)
                        {
                            this.DebugInfo("full", "Player '" + name + "' banned!");
                            if (this.ebPrintKicksToConsole == enumBoolYesNo.Yes)
                            {
                                this.PluginConsoleWrite("^bPlayer '" + name + "' found in the remote banlist! Kicking...^n");
                            }
                            this.TakeAction(name, (String)dtData.Rows[0]["reason"]);
                        }
                        else
                        {
                            this.DebugInfo("full", "Player '" + name + "' not banned!");
                            lock (this.lstCleanPlayers)
                            {
                                this.lstCleanPlayers.Add(name);
                            }
                        }
                    }
                }
                else
                {
                    this.PluginConsoleWrite("OdbcConnection could not be opened at CheckPlayerBanned!");
                }
            }
            catch (Exception e)
            {
                this.PluginConsoleWrite("Exception while CheckPlayerBanned: " + e.ToString());
            }
            finally
            {
                OdbcCon.Close();
            }
        }

        /// <summary>
        /// add or update a ban at the database
        /// </summary>
        /// <param name="ban">all availabe information for a ban</param>
        private void AddBan(CRemoteBanInfo ban)
        {
            // check if the database exists, create it if not
            if (this.strSQLHost != String.Empty && this.strSQLPort != String.Empty && this.strSQLDatabase != String.Empty && this.strSQLUserName != String.Empty && this.strSQLPassword != String.Empty)
            {
                this.CheckDatabase();
            }
            else
            {
                this.PluginConsoleWrite("ERROR: Some database-details are missing! Please add all of them, or the plugin won't work!");
                return;
            }

            OdbcConnection OdbcCon = null;

            try
            {
                OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                       "SERVER=" + this.strSQLHost + ";" +
                                                                       "PORT=" + this.strSQLPort + ";" +
                                                                       "DATABASE=" + this.strSQLDatabase + ";" +
                                                                       "UID=" + this.strSQLUserName + ";" +
                                                                       "PWD=" + this.strSQLPassword + ";" +
                                                                       "OPTION=3;");

                OdbcCon.Open();

                if (OdbcCon.State == ConnectionState.Open)
                {
                    String ip;
                    if (!String.IsNullOrEmpty(ban.IP) && ban.IP.CompareTo("") != 0 && ban.IP.Contains(":"))
                    {
                        String[] ipPort = ban.IP.Split(':');
                        ip = ipPort[0];
                    }
                    else
                    {
                        ip = ban.IP;
                    }


                    // check if the ban is in there already
                    //String sql = @"SELECT * FROM `" + this.strSQLDatabase + @"`.`remotebanlist` WHERE (SoldierName LIKE '" + ban.SoldierName + "' OR EAGUID LIKE '" + ban.EAGUID + "' OR PBGUID LIKE '" + ban.PBGUID + "' OR IP_Address LIKE '" + ip + "') AND expired NOT LIKE 'y';";
                    String sql = @"SELECT * FROM `" + this.strSQLDatabase + @"`.`remotebanlist` ";
                    bool sqlender = true;

                    if (!String.IsNullOrEmpty(ban.SoldierName))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        sql += "SoldierName LIKE '" + ban.SoldierName + "'";
                    }
                    if (!String.IsNullOrEmpty(ban.EAGUID))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " EAGUID LIKE '" + ban.EAGUID + "'";
                    }
                    if (!String.IsNullOrEmpty(ban.PBGUID))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " PBGUID LIKE '" + ban.PBGUID + "'";
                    }
                    if (!String.IsNullOrEmpty(ip))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " IP_Address LIKE '" + ip + "'";
                    }
                    sql += ") AND expired NOT LIKE 'y';";

                    this.DebugInfo("basic", sql);

                    using (OdbcCommand OdbcCommand = new OdbcCommand(sql, OdbcCon))
                    {
                        DataTable dtData = new DataTable();

                        using (OdbcDataAdapter OdbcAdapter = new OdbcDataAdapter(OdbcCommand))
                        {
                            OdbcAdapter.Fill(dtData);
                        }

                        if (dtData.Rows == null || dtData.Rows.Count == 0)
                        {
                            // ban is not in the database already, add it
                            this.DebugInfo("full", "Ban doesn't exist already, adding it...");

                            StringBuilder sbSql = new StringBuilder(@"INSERT INTO `" + this.strSQLDatabase + @"`.`remotebanlist` (");

                            if (ban.SoldierName != null && ban.SoldierName != String.Empty)
                            {
                                sbSql.Append("SoldierName, ");
                            }
                            if (ban.EAGUID != null && ban.EAGUID != String.Empty)
                            {
                                sbSql.Append("EAGUID, ");
                            }
                            if (ban.PBGUID != null && ban.PBGUID != String.Empty)
                            {
                                sbSql.Append("PBGUID, ");
                            }
                            if (ban.IP != null && ban.IP != String.Empty)
                            {
                                sbSql.Append("IP_Address, ");
                            }

                            sbSql.Append("reason, bantype, banlength, banduration, servername) VALUES ('");

                            if (ban.SoldierName != null && ban.SoldierName != String.Empty)
                            {
                                sbSql.Append(ban.SoldierName + "', '");
                            }
                            if (ban.EAGUID != null && ban.EAGUID != String.Empty)
                            {
                                sbSql.Append(ban.EAGUID + "', '");
                            }
                            if (ban.PBGUID != null && ban.PBGUID != String.Empty)
                            {
                                sbSql.Append(ban.PBGUID + "', '");
                            }
                            if (ban.IP != null && ban.IP != String.Empty)
                            {
                                sbSql.Append(ip + "', '");
                            }

                            sbSql.Append(ban.Reason + "', '");

                            switch (ban.Type)
                            {
                                case BanType.Name:
                                    sbSql.Append("name', '");
                                    break;
                                case BanType.EAGUID:
                                    sbSql.Append("eaguid', '");
                                    break;
                                case BanType.PBGUID:
                                    sbSql.Append("pbguid', '");
                                    break;
                                case BanType.IP:
                                    sbSql.Append("ip', '");
                                    break;
                                case BanType.Clan:
                                    sbSql.Append("clan', '");
                                    break;
                                default:
                                    sbSql.Append("name', '");
                                    break;
                            }

                            if (ban.Length == BanLength.Permanent)
                            {
                                sbSql.Append("perm', '");
                            }
                            else
                            {
                                sbSql.Append("seconds', '");
                            }

                            sbSql.Append(ban.Duration + "', '" + this.csiServerInfo.ServerName + "');");

                            this.DebugInfo("basic", sbSql.ToString());

                            using (OdbcCommand OdbcCommand2 = new OdbcCommand(sbSql.ToString(), OdbcCon))
                            {
                                OdbcCommand2.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            this.DebugInfo("full", "Ban already existed!");
                        }
                    }
                }
                else
                {
                    this.PluginConsoleWrite("OdbcConnection could not be opened at AddBan!");
                }
            }
            catch (Exception e)
            {
                this.PluginConsoleWrite("Exception while AddBan: " + e.ToString());
            }
            finally
            {
                OdbcCon.Close();
            }
        }

        /// <summary>
        /// remove a ban at the database
        /// </summary>
        /// <param name="ban">all availabe information for a ban</param>
        private void RemoveBan(CRemoteBanInfo ban)
        {
            // check if the database exists, create it if not
            if (this.strSQLHost != String.Empty && this.strSQLPort != String.Empty && this.strSQLDatabase != String.Empty && this.strSQLUserName != String.Empty && this.strSQLPassword != String.Empty)
            {
                this.CheckDatabase();
            }
            else
            {
                this.PluginConsoleWrite("ERROR: Some database-details are missing! Please add all of them, or the plugin won't work!");
                return;
            }

            OdbcConnection OdbcCon = null;

            try
            {
                OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                       "SERVER=" + this.strSQLHost + ";" +
                                                                       "PORT=" + this.strSQLPort + ";" +
                                                                       "DATABASE=" + this.strSQLDatabase + ";" +
                                                                       "UID=" + this.strSQLUserName + ";" +
                                                                       "PWD=" + this.strSQLPassword + ";" +
                                                                       "OPTION=3;");

                OdbcCon.Open();

                if (OdbcCon.State == ConnectionState.Open)
                {

                }
                else
                {
                    this.PluginConsoleWrite("OdbcConnection could not be opened at RemoveBan!");
                }
            }
            catch (Exception e)
            {
                this.PluginConsoleWrite("Exception while RemoveBan: " + e.ToString());
            }
            finally
            {
                OdbcCon.Close();
            }
        }



        private bool CheckBan(CRemoteBanInfo ban)
        {
            // check if the database exists, create it if not
            if (this.strSQLHost != String.Empty && this.strSQLPort != String.Empty && this.strSQLDatabase != String.Empty && this.strSQLUserName != String.Empty && this.strSQLPassword != String.Empty)
            {
                this.CheckDatabase();
            }
            else
            {
                this.PluginConsoleWrite("Please enter all database-details!");
                return false;
            }

            OdbcConnection OdbcCon = null;

            try
            {
                OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                       "SERVER=" + this.strSQLHost + ";" +
                                                                       "PORT=" + this.strSQLPort + ";" +
                                                                       "DATABASE=" + this.strSQLDatabase + ";" +
                                                                       "UID=" + this.strSQLUserName + ";" +
                                                                       "PWD=" + this.strSQLPassword + ";" +
                                                                       "OPTION=3;");

                OdbcCon.Open();

                if (OdbcCon.State == ConnectionState.Open)
                {
                    String ip;
                    if (!String.IsNullOrEmpty(ban.IP) && ban.IP.CompareTo("") != 0 && ban.IP.Contains(":"))
                    {
                        String[] ipPort = ban.IP.Split(':');
                        ip = ipPort[0];
                    }
                    else
                    {
                        ip = ban.IP;
                    }

                    String sql = @"SELECT * FROM `" + this.strSQLDatabase + @"`.`remotebanlist` ";
                    bool sqlender = true;

                    if (!String.IsNullOrEmpty(ban.SoldierName))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        sql += "SoldierName LIKE '" + ban.SoldierName + "'";
                    }
                    if (!String.IsNullOrEmpty(ban.EAGUID))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " EAGUID LIKE '" + ban.EAGUID + "'";
                    }
                    if (!String.IsNullOrEmpty(ban.PBGUID))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " PBGUID LIKE '" + ban.PBGUID + "'";
                    }
                    if (!String.IsNullOrEmpty(ip))
                    {
                        if (sqlender)
                        {
                            sql += " WHERE (";
                            sqlender = false;
                        }
                        else
                        {
                            sql += " OR ";
                        }
                        sql += " IP_Address LIKE '" + ip + "'";
                    }
                    sql += ") AND expired NOT LIKE 'y';";

                    this.DebugInfo("basic", sql);

                    using (OdbcCommand OdbcCommand = new OdbcCommand(sql, OdbcCon))
                    {
                        DataTable dtData = new DataTable();

                        using (OdbcDataAdapter OdbcAdapter = new OdbcDataAdapter(OdbcCommand))
                        {
                            OdbcAdapter.Fill(dtData);
                        }

                        if (dtData.Rows != null || dtData.Rows.Count > 0)
                        {
                            // ban is in the database
                            this.DebugInfo("full", "Ban found in the database (SQL: " + sql + ")");
                            return true;
                        }
                        else
                        {
                            this.DebugInfo("full", "Ban not found in the database (SQL: " + sql + ")");
                            return false;
                        }
                    }
                }
                else
                {
                    this.PluginConsoleWrite("OdbcConnection could not be opened at CheckBan!");
                }
            }
            catch (Exception e)
            {
                this.PluginConsoleWrite("Exception while CheckBan: " + e.ToString());
            }
            finally
            {
                OdbcCon.Close();
            }

            return false;
        }

        private string CheckBanbyName(string CheckSoldierName)
        {
            // check if the database exists, create it if not
            if (this.strSQLHost != String.Empty && this.strSQLPort != String.Empty && this.strSQLDatabase != String.Empty && this.strSQLUserName != String.Empty && this.strSQLPassword != String.Empty)
            {
                this.CheckDatabase();
            }
            else
            {
                this.PluginConsoleWrite("Please enter all database-details!");
                return "Error in setup -cant show that information";
            }

            OdbcConnection OdbcCon = null;

            try
            {
                OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                       "SERVER=" + this.strSQLHost + ";" +
                                                                       "PORT=" + this.strSQLPort + ";" +
                                                                       "DATABASE=" + this.strSQLDatabase + ";" +
                                                                       "UID=" + this.strSQLUserName + ";" +
                                                                       "PWD=" + this.strSQLPassword + ";" +
                                                                       "OPTION=3;");

                OdbcCon.Open();

                if (OdbcCon.State == ConnectionState.Open)
                {

                    String sql = @"SELECT * FROM `" + this.strSQLDatabase + @"`.`remotebanlist` ";
                    sql += " WHERE (SoldierName LIKE '" + CheckSoldierName + "') AND expired NOT LIKE 'y';";


                    this.ScaledDebugInfo(2, "SQL check is '" + sql + "' ");
                    using (OdbcCommand OdbcCommand = new OdbcCommand(sql, OdbcCon))
                    {
                        DataTable dtData = new DataTable();

                        using (OdbcDataAdapter OdbcAdapter = new OdbcDataAdapter(OdbcCommand))
                        {
                            OdbcAdapter.Fill(dtData);
                        }

                        if (dtData.Rows != null && dtData.Rows.Count > 0)
                        {
                            // ban is in the database
                            this.ScaledDebugInfo(2, "Ban found in the database (number rows " + dtData.Rows.Count + ")");
                            if (dtData.Rows.Count > 1)
                            {
                                // More than 1 result - better check with the admin

                                if (dtData.Rows.Count < 5)
                                //check number over 5 - let the admin know bad search
                                {
                                    for (int i = 1; i <= dtData.Rows.Count; i++)
                                    {
                                        NamesList = NamesList + (String)dtData.Rows[i]["reason"] + " or ";
                                    }

                                    return dtData.Rows.Count + " Players returned - please be more carefull";
                                }
                                else
                                {
                                    return "Did you mean " + NamesList + " another player ?";
                                }





                            }
                            else
                            {

                                return CheckSoldierName + " Player is banned. Reason " + (String)dtData.Rows[0]["reason"] + " by Admin " + (String)dtData.Rows[0]["banning_admin"];
                            }
                        }
                        else
                        {
                            this.ScaledDebugInfo(2, "Ban not found in the database (SQL: " + sql + ")");
                            return CheckSoldierName + " not found in bans";
                        }
                    }
                }
                else
                {
                    this.PluginConsoleWrite("OdbcConnection could not be opened at CheckBan!");
                }
            }
            catch (Exception e)
            {
                this.PluginConsoleWrite("Exception while CheckBan: " + e.ToString());
            }
            finally
            {
                OdbcCon.Close();
            }

            return "Bit if an error cant find that player";
        }

        public void DebugInfo(string debuglevel, string DebugMessage)
        {
            if (this.ebFullDebug == enumBoolYesNo.Yes)
            {
                if (debuglevel.ToLower().CompareTo("full") == 0)
                {
                    this.PluginConsoleWrite("Full debug: " + DebugMessage);
                }
            }

            if (this.ebBasicDebug == enumBoolYesNo.Yes)
            {
                if (debuglevel.ToLower().CompareTo("basic") == 0)
                {
                    this.PluginConsoleWrite("Basic debug: " + DebugMessage);
                }
            }

            // LEIBHOLD HACK ADDED
            if (debuglevel.ToLower().CompareTo("test") == 0)
            {
                this.PluginConsoleWrite("TEST: " + DebugMessage);
            }


        }


        public void ScaledDebugInfo(int debuglevel, string DebugMessage)
        {
            if (debuglevel <= ebDebugLevel)
            {
                string padout = DebugMessage.PadLeft(DebugMessage.Length + debuglevel * 5);
                this.PluginConsoleWrite(debuglevel + ": " + padout);
            }

        }

        /*
        public void externalBan( string pname, string pbguid, string peaguid, string preason)
            {

            // cylce though the external bans source listing
            // this would be a list of web page addresses
            // eg http://franksserver.com.au/webadmin/extbans.php
             //   string result;

               // fetchWebPage(result, "http://franksserver.com.au/webadmin/extbans.php");




            }



         public  void fetchWebPage( String html_data, String url)
                    {
                        try
                        {
                            if (client == null)
                            {
                                client = new WebClient();
                            }

                            html_data = client.DownloadString(url);
                            return html_data;
                        }
                        catch (WebException e)
                        {
                            if (e.Status.Equals(WebExceptionStatus.Timeout))
                                throw new Exception("HTTP request timed-out");
                            else
                                throw;

                        }
                    }


                */


        /// <summary>
        /// write a message to the plugin-console
        /// </summary>
        /// <param name="message">message to display</param>
        private void PluginConsoleWrite(String message)
        {
            String line = String.Format("^b^8Remote Banlist^0:^n {0}", message);
            this.ExecuteCommand("procon.protected.pluginconsole.write", line);
        }

        /// <summary>
        /// write an ingame-message to all players
        /// </summary>
        /// <param name="message">message to display</param>
        private void IngameSayAll(String message)
        {
            List<String> wordWrappedLines = this.WordWrap(message, 100);
            foreach (String line in wordWrappedLines)
            {
                String formattedLine = String.Format("[RemoteBanlist] {0}", line);
                this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "all");
            }
        }

        private void IngameSayTo(String message, String whoto)
        {


            List<String> wordWrappedLines = this.WordWrap(message, 100);
            foreach (String line in wordWrappedLines)
            {
                String formattedLine = String.Format("[RemoteBanlist] {0}", line);
                if (whoto == "all")
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, whoto);
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "player", whoto);
                }


            }
        }

        /// <summary>
        /// write an ingame-message to a specific squad 
        /// </summary>
        /// <param name="message">message to displayer</param>
        /// <param name="teamid">ID of the desired team</param>
        /// <param name="squadid">ID of the desired squad</param>
        private string SQLcleanup(String SQLmessage)

        {
            this.ScaledDebugInfo(6, "Ban reason Cleanup - Entered");
            // Replace invalid characters with empty strings.
            return Regex.Replace(SQLmessage, @"['\\/]", "");
        }



        /// <summary>
        /// write an ingame-message to a specific squad 
        /// </summary>
        /// <param name="message">message to displayer</param>
        /// <param name="teamid">ID of the desired team</param>
        /// <param name="squadid">ID of the desired squad</param>
        private void IngameSaySquad(String message, int teamid, int squadid)
        {
            List<String> wordWrappedLines = this.WordWrap(message, 100);
            foreach (String line in wordWrappedLines)
            {
                String formattedLine = String.Format("[RemoteBanlist] {0}", line);
                this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "squad", teamid.ToString(), squadid.ToString());
            }
        }

        #endregion

        #region own classes & enums

        /// <summary>
        /// enumeration for setting the type of a ban
        /// </summary>
        private enum BanType { Name, IP, Clan, EAGUID, PBGUID }

        /// <summary>
        /// enumeration for setting the length of a ban
        /// </summary>
        private enum BanLength { Permanent, Seconds }


        public class CPBBanInfo
        {

            private String strPBGUID;
            private String strSoldierName;
            private int strBanCount;
            private DateTime strTimeStamp;

            /// <summary>
            /// create a new CPBBanInfo and initialise all variables with empty values
            /// </summary>
            public CPBBanInfo(string PBGUID)
            {
                this.strPBGUID = PBGUID;
                this.strSoldierName = null;
                this.strBanCount = 1;
                this.strTimeStamp = DateTime.Now;
            }


            /// <summary>
            /// PB GUID of the banned player
            /// </summary>
            public String PBGUID { get { return this.strPBGUID; } set { this.strPBGUID = value; } }
            /// <summary>
            /// soldiername of the banned player
            /// </summary>
            public String SoldierName { get { return this.strSoldierName; } set { this.strSoldierName = value; } }
            /// <summary>
            /// times we have seen the ban
            /// </summary>
            public int BanCount { get { return this.strBanCount; } set { this.strBanCount = value; } }
            /// <summary>
            /// timestamp of the ban-creation
            /// </summary>
            public DateTime TimeStamp { get { return this.strTimeStamp; } set { this.strTimeStamp = value; } }
        }


        /// <summary>
        /// holds additional information about a ban, designed to be stored in a MySQL-database
        /// </summary>
        private class CRemoteBanInfo
        {
            private String strClanTag;
            private String strSoldierName;
            private String strEAGUID;
            private String strPBGUID;
            private String strIP;
            private String strReason;
            private String strComment;
            private String strBanningAdmin;
            private BanType btType;
            private BanLength blLength;
            private String strDuration;
            private byte[] bPBSS;
            private String strTimeStamp;

            /// <summary>
            /// create a new RemoteBanInfo-object and initialise all variables with empty values
            /// </summary>
            public CRemoteBanInfo()
            {
                this.strClanTag = null;
                this.strSoldierName = null;
                this.strEAGUID = null;
                this.strPBGUID = null;
                this.strIP = null;
                this.strReason = null;
                this.strComment = null;
                this.strBanningAdmin = null;
                this.btType = BanType.Name;
                this.blLength = BanLength.Permanent;
                this.strDuration = "0";
                this.bPBSS = null;
                this.strTimeStamp = null;
            }

            /// <summary>
            /// clantag of the banned player
            /// </summary>
            public String ClanTag { get { return this.strClanTag; } set { this.strClanTag = value; } }
            /// <summary>
            /// soldiername of the banned player
            /// </summary>
            public String SoldierName { get { return this.strSoldierName; } set { this.strSoldierName = value; } }
            /// <summary>
            /// EA GUID of the banned player
            /// </summary>
            public String EAGUID { get { return this.strEAGUID; } set { this.strEAGUID = value; } }
            /// <summary>
            /// PB GUID of the banned player
            /// </summary>
            public String PBGUID { get { return this.strPBGUID; } set { this.strPBGUID = value; } }
            /// <summary>
            /// IP of the banned player
            /// </summary>
            public String IP { get { return this.strIP; } set { this.strIP = value; } }
            /// <summary>
            /// reason for banning
            /// </summary>
            public String Reason { get { return this.strReason; } set { this.strReason = value; } }
            /// <summary>
            /// additional comment by an admin
            /// </summary>
            public String Comment { get { return this.strComment; } set { this.strComment = value; } }
            /// <summary>
            /// name of the admin issueing the ban
            /// </summary>
            public String BanningAdmin { get { return this.strBanningAdmin; } set { this.strBanningAdmin = value; } }
            /// <summary>
            /// type of the ban
            /// </summary>
            public BanType Type { get { return this.btType; } set { this.btType = value; } }
            /// <summary>
            /// length of the ban
            /// </summary>
            public BanLength Length { get { return this.blLength; } set { this.blLength = value; } }
            /// <summary>
            /// duration of the ban (empty, seconds or timestamp)
            /// </summary>
            public String Duration { get { return this.strDuration; } set { this.strDuration = value; } }
            /// <summary>
            /// bytes of the latest Punkbuster-Screenshot
            /// </summary>
            public byte[] PBSS { get { return this.bPBSS; } set { this.bPBSS = value; } }
            /// <summary>
            /// timestamp of the ban-creation
            /// </summary>
            public String TimeStamp { get { return this.strTimeStamp; } set { this.strTimeStamp = value; } }
        }



        #endregion

    }
}
