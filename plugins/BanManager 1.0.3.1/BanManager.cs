/*  Copyright 2012 'DaMagicWobber'

    Big Thank's to MorpheusX(AUT) (Website: http://www.phogue.net/forumvb/member.php?565-MorpheusX(AUT))
	and leibhold (Website: http://www.phogue.net/forumvb/member.php?849-leibhold) 
	for the hard work on the plugin and the Webpage
 
 *	DaMagicWoBBeR (Website: http://www.phogue.net/forumvb/member.php?15149-DaMagicWoBBeR)
 
    DaMagicWobber Plugins for Procon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    DaMagicWoBBeR Plugins for Procon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with DaMagicWobber Plugins for Procon.  If not, see <http://www.gnu.org/licenses/>.

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

namespace PRoConEvents
{
    public class BanManager : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region variables & constructor

        #region connection information

        /// <summary>
        /// odbc driver version that should be used
        /// </summary>
        private String strDatabaseDriver;
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
        /// dictionary of all current players and their CRemotebanInfo-objects, using the playername as a key
        /// </summary>
        private Dictionary<String, CRemoteBanInfo> dicCRemoteBanInfo;
        /// <summary>
        /// list of all current players who have been checked already and are not banned
        /// </summary>
        private List<String> lstCleanPlayers;
        /// <summary>
        /// CServerInfo-object for the current connection
        /// </summary>
        private CServerInfo csiServerInfo;
        /// <summary>
        /// used to store the previous sent chat-message to prevent spam
        /// </summary>
        private String strPreviousMessage;
        /// <summary>
        /// used to store the previous speaker to prevent spam
        /// </summary>
        private String strPreviousSpeaker;

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
		private enumBoolYesNo ebEnableIngameAdmin;
        // LEIBHOLD HACK ADDED
        private enumBoolYesNo ebTestrun;
        private enumBoolYesNo ebEmptyServerPBBanlist;
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
        /// <summary>
        /// toggles whether the plugin react on Punktbuster Kicks/Bans
        /// </summary>
        private enumBoolYesNo ebPBDisable;
        /// <summary>
        /// defines the Servergroup the Server belongs to (0 is global ban)
        /// </summary>>
        private int servergroup;
        //debug level 1 = min 9 = verbose
        private int ebDebugLevel;
        /// <summary>
        /// sly-command used by the admin
        /// </summary>
        private string strSlayCommand;
        /// <summary>
        /// kick-command used by the admin
        /// </summary>
        private string strKickCommand;
        /// <summary>
        /// Roundban-command used by the admin
        /// </summary>
        private string strRoundBanCommand;
        /// <summary>
        /// Timeban-command used by the admin
        /// </summary>
        private string strTimeBanCommand;
        /// <summary>
        /// ban-command used by the admin
        /// </summary>
        private string strBanCommand;
        /// <summary>
        /// unban-command used by the admin
        /// </summary>
        private string strUnBanCommand;
        /// <summary>
        /// globalban-command used by the admin
        /// </summary>
        private string strGlobalBanCommand;
        /// <summary>
        /// send returned ban information to all players or just asker
        /// </summary
        private enumBoolYesNo SendBanToAll;

        #endregion

        #region others

        /// <summary>
        /// states whether the plugins is enabled or disabled
        /// </summary>
        private OdbcConnection OdbcCon;
        private String banningadmin;
        private TimeoutSubset tsBanLength;
        private readonly Object Locker;

        #endregion

        public BanManager()
        {
            this.dicCRemoteBanInfo = new Dictionary<string, CRemoteBanInfo>();
            this.lstCleanPlayers = new List<String>();
            this.strPreviousMessage = String.Empty;
            this.strPreviousSpeaker = String.Empty;

            this.strDatabaseDriver = "MySQL ODBC 5.1 Driver";
            this.strSQLHost = String.Empty;
            this.strSQLPort = "3306";
            this.strSQLUserName = String.Empty;
            this.strSQLPassword = String.Empty;
            this.strSQLDatabase = String.Empty;

            this.ebEmptyServerBanlist = enumBoolYesNo.No;
            this.ebEnableIngameAdmin = enumBoolYesNo.Yes;
            this.ebTestrun = enumBoolYesNo.Yes;
            this.ebEmptyServerPBBanlist = enumBoolYesNo.No;
            this.ebPrintKicksToConsole = enumBoolYesNo.Yes;
            this.ebBasicDebug = enumBoolYesNo.No;
            this.ebFullDebug = enumBoolYesNo.No;
            this.ebPBDisable = enumBoolYesNo.No;
            this.servergroup = 1;
            this.ebDebugLevel = 0;
            this.strSlayCommand = "!s";
            this.strKickCommand = "!k";
            this.strRoundBanCommand = "!rb";
            this.strGlobalBanCommand = "!gb";
            this.strBanCommand = "!b";
            this.strUnBanCommand = "!ub";
            this.strTimeBanCommand = "!tb";
            //this.blPluginEnabled = false;
            this.banningadmin = "BanList Manager";
            Locker = new Object();
            this.OdbcCon = null;
        }

        #endregion

        #region plugin details & settings

        public String GetPluginName()
        {
            return "Ban Manager";
        }

        public String GetPluginVersion()
        {
            return "1.0.3.1";
        }

        public String GetPluginAuthor()
        {
            return "DaMagicWoBBeR";
        }

        public String GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/member.php?15149-DaMagicWoBBeR";
        }

        public String GetPluginDescription()
        {
            return @"
            <h2>Description</h2>
            <p><b>Banlist Manager</b> is a plugin based on MorpheusX(AUT) CRemoteBanlist V 1.0.0.11 (Website: http://www.phogue.net/forumvb/member.php?565-MorpheusX(AUT)). <br />
            Making use of a MySQL-Datebase, the plugin stores all its ban-information independent from the BF3-Server and it's banlists, also providing some more fields to add information like 'Banning Admin' and 'Server Name'.<br />
            Bans can either be viewed or edited in the database directly. It implements its own Ingame Admin Commands. Please make use of it, if you want banning admin information in the database, bans taken via Procon can't provide the banning admin.<br />
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
			<p><i>If activated, the plugin issues a 'banList.clear' and 'banList.save' command after adding all bans in the list, thus keeping the local banlists clean.</i> <br><b>NOTE: ONLY DEACTIVATE FOR TESTPURPOSE!</b></p>
			</blockquote>
			<blockquote><h4>Test Run?</h4>
			<p><i>If activated, the plugin will only print a kick Notification to the plugin console, but will not kick the player who are in the banlist</i></p>
			</blockquote>
			<blockquote><h4>Keep server-PBbanlist empty?</h4>
			<p><i>If activated, PBbanlist will be cleared after adding the bans to the database </i><br><b>Note: Plugin will not add all PBBans if deactivated and there are more than about 100 Entries. ONLY DEACTIVATE FOR TESTPURPOSE!</b></p>
			</blockquote>
			<blockquote><h4>disable Punkbuster support?</h4>
			<p><i>If set to yes, no Punkbuster Bans/Tempbans will be added to the database, but they are still working!</b></p>
			</blockquote>
            <blockquote><h4>Print kicked players to pluginconsole?</h4>
			<p><i>Prints a message to the pluginconsole, stating that a specific player has been found when kicking him.</i></p>
			</blockquote>
			<blockquote><h4>Server Group?</h4>
			<p><i>If 0 all bans will forced to every Gameserver, else only to this servergroup</i></p>
			</blockquote>
            <blockquote><h4>Debug level</h4>
			<p><i>0 is no debug,  1 = min 9 = verbose messages to plugin-console</i></p>
			</blockquote>
            <blockquote><h4>Enable Ingame Commands</h4>
			<p><i>TO GET ALL FEATURES OF THIS PLUGIN FORCE ALL BANS WITH THE INGAME COMMANDS OF THIS PLUGIN e.g. BANNING ADMIN</i><br><b>SYNTAXE: [command] [substring of the player] [optional: reason]</b></p>
			</blockquote>
			<blockquote><h4>Global Bancommand</h4>
			<p><i>If you uses more servergroups and wants to do a ban over all servers then use this command, it will set the servergroup to 0 for this ban</i></p>
			</blockquote>
			<blockquote><h4>Unban Command</h4>
			<p><i>Remove a ban from the database if the ban is global or corresponding to this servergroup. You need the excat name to unban. If a banentry is without the name in the database it can't be removed with this command</i><br><b>SYNTAXE: [command] [exact playername]</b></p>
			</blockquote>
            <h2>Changelog</h2>
			<h4>Version 1.0.3.1 </h4>
			<p>- fixed spamming FullDebug into PlugIn Console</br></p>
			<h4>Version 1.0.3.0 </h4>
			<p>- fixed a bug where ingame commands doesn't work</br>
			- fixed PunkBuster Problem where the wrong BanLength Type got written into database</br>
			- added option to disable Punkbuster Bans written into database</br>
			- added support for MySQL ODBC Driver 5.2</br></p>
			<h4>Version 1.0.2.0 </h4>
			<p>- compatible with Webpage</br>
			- add one more table to database</br>
			- change default value from eaguid to guid in database bantype</br>
			- change default value from 1 to 0 in database servergroup</br></p>
			<h4>Version 1.0.0.0 </h4>
			<p>- first release</p>
			<h2>Known Bugs</h2>
            <blockquote><h4>IP-Bans will not forced and not added to the Database maybe in a later version </h4>";
        }

        public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
        {
            this.strHostName = strHostName;
            this.strPort = strPort;
            this.strPRoConVersion = strPRoConVersion;

            this.RegisterEvents(this.GetType().Name, "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnListPlayers", "OnServerInfo", "OnLevelLoaded", "OnBanAdded", "OnBanRemoved", "OnBanList", "OnPunkbusterBanInfo", "OnPunkbusterPlayerInfo", "OnPlayerLeft", "OnPlayerJoin");

        }

        public void OnPluginEnable()
        {
            CheckDatabase();
            FrostbitePlayerInfoList.Clear();
            PunkbusterPlayerInfoList.Clear();
            lstCleanPlayers.Clear();
            ExecuteCommand("procon.protected.pluginconsole.write", "^bBanList Manager: ^2Enabled!");
            ExecuteCommand("procon.protected.send", "serverInfo");
            ExecuteCommand("procon.protected.send", "banList.list");
            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            ExecuteCommand("procon.protected.tasks.add", "ProconPBList", "1", "90", "-1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_banlist");

        }

        public void OnPluginDisable()
        {
            FrostbitePlayerInfoList.Clear();
            PunkbusterPlayerInfoList.Clear();
            lstCleanPlayers.Clear();
            ExecuteCommand("procon.protected.tasks.remove", "ProconPBLList");
            ExecuteCommand("procon.protected.tasks.remove", "banempty");
            ExecuteCommand("procon.protected.tasks.remove", "banload");
            ExecuteCommand("procon.protected.pluginconsole.write", "^bBanList Manager: ^1Disabled =(");
            OdbcCon.Close();
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1. MySQL Settings|ODBC Driver Version", "enum.Drivers(MySQL ODBC 5.1 Driver|MySQL ODBC 5.2 Driver)", strDatabaseDriver));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Host", typeof(String), this.strSQLHost));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Port", typeof(String), this.strSQLPort));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Username", typeof(String), this.strSQLUserName));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Password", typeof(String), this.strSQLPassword));
            lstReturn.Add(new CPluginVariable("1. MySQL Settings|MySQL Database", typeof(String), this.strSQLDatabase));

            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Keep server-banlist empty?", typeof(enumBoolYesNo), ebEmptyServerBanlist));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Test Run?", typeof(enumBoolYesNo), ebTestrun));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Keep server-PBbanlist empty?", typeof(enumBoolYesNo), ebEmptyServerPBBanlist));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Disable Support for PB-Bans?", typeof(enumBoolYesNo), ebPBDisable));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Print kicked players to pluginconsole?", typeof(enumBoolYesNo), ebPrintKicksToConsole));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Server Group", this.servergroup.GetType(), this.servergroup));
            lstReturn.Add(new CPluginVariable("2. Plugin Settings|Debug Level?", this.ebDebugLevel.GetType(), this.ebDebugLevel));
            lstReturn.Add(new CPluginVariable("3. Ingame Admin|Enable Ingame Commands?", typeof(enumBoolYesNo), this.ebEnableIngameAdmin));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame Slaycommand", typeof(string), this.strSlayCommand));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame Kickcommand", typeof(string), this.strKickCommand));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame RoundBancommand", typeof(string), this.strRoundBanCommand));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame TimeBancommand", typeof(string), this.strTimeBanCommand));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame Bancommand", typeof(string), this.strBanCommand));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame GlobalBancommand", typeof(string), this.strGlobalBanCommand));
            lstReturn.Add(new CPluginVariable("3. InGame Admin|Ingame UnBancommand", typeof(string), this.strUnBanCommand));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("ODBC Driver Version", "enum.Drivers(MySQL ODBC 5.1 Driver|MySQL ODBC 5.2 Driver)", strDatabaseDriver));
            lstReturn.Add(new CPluginVariable("MySQL Host", typeof(String), this.strSQLHost));
            lstReturn.Add(new CPluginVariable("MySQL Port", typeof(String), this.strSQLPort));
            lstReturn.Add(new CPluginVariable("MySQL Username", typeof(String), this.strSQLUserName));
            lstReturn.Add(new CPluginVariable("MySQL Password", typeof(String), this.strSQLPassword));
            lstReturn.Add(new CPluginVariable("MySQL Database", typeof(String), this.strSQLDatabase));

            lstReturn.Add(new CPluginVariable("Keep server-banlist empty?", typeof(enumBoolYesNo), ebEmptyServerBanlist));
            lstReturn.Add(new CPluginVariable("Test Run?", typeof(enumBoolYesNo), ebTestrun));
            lstReturn.Add(new CPluginVariable("Keep server-PBbanlist empty?", typeof(enumBoolYesNo), ebEmptyServerPBBanlist));
            lstReturn.Add(new CPluginVariable("Disable Support for PB-Bans?", typeof(enumBoolYesNo), ebPBDisable));
            lstReturn.Add(new CPluginVariable("Print kicked players to pluginconsole?", typeof(enumBoolYesNo), ebPrintKicksToConsole));
            lstReturn.Add(new CPluginVariable("Server Group", this.servergroup.GetType(), this.servergroup));
            lstReturn.Add(new CPluginVariable("Debug Level?", this.ebDebugLevel.GetType(), this.ebDebugLevel));
            lstReturn.Add(new CPluginVariable("Enable Ingame Commands?", typeof(enumBoolYesNo), this.ebEnableIngameAdmin));
            lstReturn.Add(new CPluginVariable("Ingame Slaycommand", typeof(string), this.strSlayCommand));
            lstReturn.Add(new CPluginVariable("Ingame Kickcommand", typeof(string), this.strKickCommand));
            lstReturn.Add(new CPluginVariable("Ingame RoundBancommand", typeof(string), this.strRoundBanCommand));
            lstReturn.Add(new CPluginVariable("Ingame TimeBancommand", typeof(string), this.strTimeBanCommand));
            lstReturn.Add(new CPluginVariable("Ingame Bancommand", typeof(string), this.strBanCommand));
            lstReturn.Add(new CPluginVariable("Ingame GlobalBancommand", typeof(string), this.strGlobalBanCommand));
            lstReturn.Add(new CPluginVariable("Ingame UnBancommand", typeof(string), this.strUnBanCommand));
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
            else if (strVariable.CompareTo("ODBC Driver Version") == 0)
            {
                this.strDatabaseDriver = strValue;
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
            else if (strVariable.CompareTo("Enable Ingame Commands?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebEnableIngameAdmin = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Test Run?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebTestrun = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Keep server-PBbanlist empty?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebEmptyServerPBBanlist = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Disable Support for PB-Bans?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                ebPBDisable = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Print kicked players to pluginconsole?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.ebPrintKicksToConsole = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Server Group") == 0 && int.TryParse(strValue, out iTmp) == true)
            {
                this.servergroup = iTmp;
            }
            else if (strVariable.CompareTo("Debug Level?") == 0 && int.TryParse(strValue, out iTmp) == true)
            {
                this.ebDebugLevel = iTmp;
            }
            else if (strVariable.CompareTo("Ingame Slaycommand") == 0)
            {
                this.strSlayCommand = strValue;
            }
            else if (strVariable.CompareTo("Ingame Kickcommand") == 0)
            {
                this.strKickCommand = strValue;
            }
            else if (strVariable.CompareTo("Ingame RoundBancommand") == 0)
            {
                this.strRoundBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Ingame TimeBancommand") == 0)
            {
                this.strTimeBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Ingame Bancommand") == 0)
            {
                this.strBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Ingame GlobalBancommand") == 0)
            {
                this.strGlobalBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Ingame UnBancommand") == 0)
            {
                this.strUnBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Send Information Ban To All") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue))
            {
                this.SendBanToAll = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
        }

        #endregion

        #region events

        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.csiServerInfo = csiServerInfo;
        }

        public override void OnPlayerJoin(string soldierName)
        {
            this.ScaledDebugInfo(1, "OnPlayerJoin fired");
            if (!String.IsNullOrEmpty(soldierName))
            {
                lock (Locker)
                {
                    base.OnPlayerJoin(soldierName);
                    if (lstCleanPlayers.Contains(soldierName)) lstCleanPlayers.Remove(soldierName);
                    string reason = CheckPlayerBanned(soldierName, "", "");
                }
            }
            this.ScaledDebugInfo(2, "OnPlayerJoin left");
        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            this.ScaledDebugInfo(1, "OnPlayerLeft fired");
            if (!String.IsNullOrEmpty(playerInfo.SoldierName))
            {
                lock (Locker)
                {
                    base.OnPlayerLeft(playerInfo);
                    if (!String.IsNullOrEmpty(playerInfo.SoldierName))
                    {
                        if (lstCleanPlayers.Contains(playerInfo.SoldierName)) lstCleanPlayers.Remove(playerInfo.SoldierName);
                    }
                }
            }
            this.ScaledDebugInfo(2, "OnPlayerLeft left");
        }

        public override void OnListPlayers(List<CPlayerInfo> playerlist, CPlayerSubset cpsSubset)
        {
            this.ScaledDebugInfo(1, "OnListPlayers fired");

            lock (Locker)
            {
                base.OnListPlayers(playerlist, cpsSubset);

                if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
                {
                    #region checkplayerlists
                    // Remove stored players not on the server from FrostbitePlayerInfoList.    
                    if (FrostbitePlayerInfoList.Count > playerlist.Count)
                    {
                        foreach (KeyValuePair<string, CPlayerInfo> StoredPlayerInfo in FrostbitePlayerInfoList)
                        {
                            bool withinlist = false;
                            {
                                foreach (CPlayerInfo playeronserver in playerlist)
                                {
                                    if (playeronserver.SoldierName == StoredPlayerInfo.Key)
                                    {
                                        this.ScaledDebugInfo(3, StoredPlayerInfo.Value.SoldierName + " found in Playerlist");
                                        withinlist = true;
                                        break;
                                    }
                                }
                                if (!withinlist)
                                {
                                    this.ScaledDebugInfo(3, StoredPlayerInfo.Value.SoldierName + " not found in Playerlist and removed");
                                    this.FrostbitePlayerInfoList.Remove(StoredPlayerInfo.Key);
                                }
                            }
                        }
                    }
                    // Remove stored players not on the server from PunkbusterPlayerInfoList. 
                    if (PunkbusterPlayerInfoList.Count > playerlist.Count)
                    {
                        foreach (KeyValuePair<string, CPunkbusterInfo> StoredPlayerInfo in PunkbusterPlayerInfoList)
                        {
                            bool withinlist = false;
                            {
                                foreach (CPlayerInfo playeronserver in playerlist)
                                {
                                    if (playeronserver.SoldierName == StoredPlayerInfo.Key)
                                    {
                                        this.ScaledDebugInfo(3, StoredPlayerInfo.Value.SoldierName + " found in PunkBusterPlayerlist");
                                        withinlist = true;
                                        break;
                                    }
                                }
                                if (!withinlist)
                                {
                                    this.ScaledDebugInfo(3, StoredPlayerInfo.Value.SoldierName + " not found in PunkBusterPlayerlist and removed");
                                    this.PunkbusterPlayerInfoList.Remove(StoredPlayerInfo.Key);
                                }
                            }
                        }
                    }
                    // Remove stored players not on the server from lstCleanplayers. 
                    if (lstCleanPlayers.Count > playerlist.Count)
                    {
                        foreach (string StoredPlayerInfo in lstCleanPlayers)
                        {
                            bool withinlist = false;
                            {
                                foreach (CPlayerInfo playeronserver in playerlist)
                                {
                                    if (playeronserver.SoldierName == StoredPlayerInfo)
                                    {
                                        this.ScaledDebugInfo(3, StoredPlayerInfo + " found in CleanPlayerlist");
                                        withinlist = true;
                                        break;
                                    }
                                }
                                if (!withinlist)
                                {
                                    this.ScaledDebugInfo(3, StoredPlayerInfo + " not found in CleanPlayerlist and removed");
                                    this.lstCleanPlayers.Remove(StoredPlayerInfo);
                                }
                            }
                        }
                    }
                    #endregion checkplayerlists
                    foreach (CPlayerInfo player in playerlist)
                    {
                        // use a boolean as a trigger so the lock around lstCleanPlayers doesn't lead to a deadlock when calling CheckPlayerBanned
                        bool blChecked = false;
                        // see if a player has been checked already, skipping him if he's "clean"
                        if (!String.IsNullOrEmpty(player.SoldierName))
                        {
                            blChecked = lstCleanPlayers.Contains(player.SoldierName);
                            if (!blChecked)
                            {

                                if (CheckPlayerBanned(player.SoldierName, player.GUID, String.Empty) == null)
                                {
                                    if (PunkbusterPlayerInfoList.ContainsKey(player.SoldierName) && !String.IsNullOrEmpty(PunkbusterPlayerInfoList[player.SoldierName].GUID)
                                        && FrostbitePlayerInfoList.ContainsKey(player.SoldierName) && !String.IsNullOrEmpty(FrostbitePlayerInfoList[player.SoldierName].GUID))
                                    {
                                        DebugInfo("full", "test passed");
                                        lstCleanPlayers.Add(player.SoldierName);
                                    }
                                }
                            }
                            else DebugInfo("full", "player already checked");
                        }
                    }
                }
            }
            this.ScaledDebugInfo(2, "OnListPlayers left");
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo pbplayer)
        {
            this.ScaledDebugInfo(1, "OnPunkbusterPlayerInfo fired");
            if (!String.IsNullOrEmpty(pbplayer.SoldierName))
            {
                lock (Locker)
                {
                    base.OnPunkbusterPlayerInfo(pbplayer);
                }
            }
            this.ScaledDebugInfo(2, "OnPunkbusterPlayerInfo left");
        }

        public override void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if (ebEnableIngameAdmin == enumBoolYesNo.Yes)
            {
                ScaledDebugInfo(1, "OnGlobalChat fired");

                // just filter all messages containing the bancommand and not being send by the server
                ScaledDebugInfo(2, "OnGlobalChat fired from " + strSpeaker + " Message: " + strMessage);

                if (!String.IsNullOrEmpty(strSpeaker) && strSpeaker.ToLower() != "server")
                {
                    lock (Locker)
                    {
                        strMessage.Trim();
                        if (!String.IsNullOrEmpty(strMessage))
                        {
                            string[] ingamecommand = strMessage.Split(' ');
                            int elements = ingamecommand.GetLength(0);
                            if (elements >= 2)
                            {

                                #region checkunbanncommand
                                if (GetAccountPrivileges(strSpeaker).CanEditBanList && ingamecommand[0].ToLower() == strUnBanCommand.ToLower())
                                {
                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        string unbanned = RemoveBan(ingamecommand[1].Trim());
                                        ExecuteCommand("procon.protected.send", "admin.say", unbanned, "player", strSpeaker);
                                    }
                                }

                                else if (!GetAccountPrivileges(strSpeaker).CanEditBanList && (ingamecommand[0].ToLower() == strUnBanCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to unban !!!", "player", strSpeaker);
                                    return;
                                }
                                #endregion

                                #region checkbanncommand
                                if (GetAccountPrivileges(strSpeaker).CanPermanentlyBanPlayers && ingamecommand[0].ToLower() == strBanCommand.ToLower())
                                {
                                    int playercount = 0;
                                    string SoldierName = String.Empty;
                                    CBanInfo victim = new CBanInfo(String.Empty, String.Empty);
                                    string chatreason = String.Empty;

                                    for (int i = 2; i < elements; i++)
                                    {
                                        chatreason += SQLcleanup(ingamecommand[i].Trim()) + " ";
                                    }

                                    TimeoutSubset test = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Permanent);

                                    tsBanLength = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Permanent);

                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        ingamecommand[1].Trim();


                                        foreach (KeyValuePair<string, CPlayerInfo> player in FrostbitePlayerInfoList)
                                        {
                                            if (player.Key.ToLower().Contains(ingamecommand[1].ToLower()))
                                            {
                                                playercount++;
                                                SoldierName = player.Value.SoldierName;
                                                victim = new CBanInfo("guid", player.Value.GUID, tsBanLength, chatreason);
                                            }
                                        }
                                    }

                                    switch (playercount)
                                    {
                                        case 0:

                                            this.ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " not found!!!", "player", strSpeaker);
                                            return;

                                        case 1:

                                            banningadmin = strSpeaker;
                                            if (AddBan(victim))
                                            {
                                                TakeAction(SoldierName, chatreason, TimeoutSubset.TimeoutSubsetType.Permanent);
                                                ExecuteCommand("procon.protected.send", "admin.say", SoldierName + " got banned for " + chatreason, "all");
                                                banningadmin = "";
                                                return;
                                            }
                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " could not force ban", "player", strSpeaker);
                                            return;

                                        default:

                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " is not unique!!! ", "player", strSpeaker);
                                            return;
                                    }
                                }

                                else if (!GetAccountPrivileges(strSpeaker).CanPermanentlyBanPlayers && (ingamecommand[0].ToLower() == strBanCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to ban !!!", "player", strSpeaker);
                                    DebugInfo("full", "Line665");
                                    return;
                                }
                                #endregion

                                #region checkglobalbanncommand
                                if (GetAccountPrivileges(strSpeaker).CanPermanentlyBanPlayers && ingamecommand[0].ToLower() == strGlobalBanCommand.ToLower())
                                {
                                    int playercount = 0;
                                    string SoldierName = "";
                                    CBanInfo victim = new CBanInfo("", "");
                                    string chatreason = "";

                                    for (int i = 2; i < elements; i++)
                                    {
                                        chatreason += SQLcleanup(ingamecommand[i].Trim()) + " ";
                                    }

                                    TimeoutSubset test = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Permanent);

                                    tsBanLength = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Permanent);

                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        ingamecommand[1].Trim();


                                        foreach (KeyValuePair<string, CPlayerInfo> player in FrostbitePlayerInfoList)
                                        {
                                            if (player.Key.ToLower().Contains(ingamecommand[1].ToLower()))
                                            {
                                                playercount++;
                                                SoldierName = player.Value.SoldierName;
                                                victim = new CBanInfo("guid", player.Value.GUID, tsBanLength, chatreason);
                                            }
                                        }
                                    }

                                    switch (playercount)
                                    {
                                        case 0:

                                            this.ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " not found!!!", "player", strSpeaker);
                                            return;

                                        case 1:
                                            int tempservergroup = servergroup;
                                            servergroup = 0;
                                            banningadmin = strSpeaker;
                                            if (AddBan(victim))
                                            {
                                                TakeAction(SoldierName, chatreason, TimeoutSubset.TimeoutSubsetType.Permanent);
                                                ExecuteCommand("procon.protected.send", "admin.say", SoldierName + " got banned for " + chatreason, "all");
                                                banningadmin = "";
                                                servergroup = tempservergroup;
                                                return;
                                            }
                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " could not force ban", "player", strSpeaker);
                                            return;

                                        default:

                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " is not unique!!! ", "player", strSpeaker);
                                            return;
                                    }
                                }

                                else if (!GetAccountPrivileges(strSpeaker).CanPermanentlyBanPlayers && (ingamecommand[0].ToLower() == strGlobalBanCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to ban !!!", "player", strSpeaker);
                                    DebugInfo("full", "Line665");
                                    return;
                                }
                                #endregion

                                #region checkroundbanncommand
                                if (GetAccountPrivileges(strSpeaker).CanTemporaryBanPlayers && ingamecommand[0].ToLower() == strRoundBanCommand.ToLower())
                                {
                                    int playercount = 0;
                                    string SoldierName = "";
                                    CBanInfo victim = new CBanInfo("", "");
                                    string chatreason = "";

                                    for (int i = 2; i < elements; i++)
                                    {
                                        chatreason += SQLcleanup(ingamecommand[i].Trim()) + " ";
                                    }

                                    TimeoutSubset test = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Round);

                                    tsBanLength = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Round);

                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        ingamecommand[1].Trim();


                                        foreach (KeyValuePair<string, CPlayerInfo> player in FrostbitePlayerInfoList)
                                        {
                                            if (player.Key.ToLower().Contains(ingamecommand[1].ToLower()))
                                            {
                                                playercount++;
                                                SoldierName = player.Value.SoldierName;
                                                victim = new CBanInfo("guid", player.Value.GUID, tsBanLength, chatreason);
                                            }
                                        }
                                    }

                                    switch (playercount)
                                    {
                                        case 0:

                                            this.ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " not found!!!", "player", strSpeaker);
                                            return;

                                        case 1:

                                            banningadmin = strSpeaker;
                                            if (AddBan(victim))
                                            {
                                                TakeAction(SoldierName, chatreason, TimeoutSubset.TimeoutSubsetType.Round);
                                                ExecuteCommand("procon.protected.send", "admin.say", SoldierName + " got roundbanned for " + chatreason, "all");
                                                banningadmin = "";
                                                return;
                                            }
                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " could not force roundban", "player", strSpeaker);
                                            return;

                                        default:

                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " is not unique!!! ", "player", strSpeaker);
                                            return;
                                    }
                                }

                                else if (!GetAccountPrivileges(strSpeaker).CanTemporaryBanPlayers && (ingamecommand[0].ToLower() == strRoundBanCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to roundban !!!", "player", strSpeaker);
                                    DebugInfo("full", "Line665");
                                    return;
                                }
                                #endregion

                                #region checkkickcommand
                                if (GetAccountPrivileges(strSpeaker).CanKickPlayers && ingamecommand[0].ToLower() == strKickCommand.ToLower())
                                {
                                    int playercount = 0;
                                    string SoldierName = String.Empty;
                                    string chatreason = String.Empty;

                                    for (int i = 2; i < elements; i++)
                                    {
                                        chatreason += SQLcleanup(ingamecommand[i].Trim()) + " ";
                                    }

                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        foreach (KeyValuePair<string, CPlayerInfo> player in FrostbitePlayerInfoList)
                                        {
                                            if (!String.IsNullOrEmpty(ingamecommand[1]))
                                            {
                                                if (player.Key.ToLower().Contains(ingamecommand[1].ToLower()))
                                                {
                                                    SoldierName = player.Value.SoldierName;
                                                    playercount++;
                                                }
                                            }
                                        }
                                    }
                                    switch (playercount)
                                    {
                                        case 0:

                                            this.ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " not found!!!", "player", strSpeaker);
                                            return;

                                        case 1:

                                            TakeAction(SoldierName, chatreason, TimeoutSubset.TimeoutSubsetType.None);
                                            ExecuteCommand("procon.protected.send", "admin.say", SoldierName + " got kicked for " + chatreason, "all");
                                            return;

                                        default:

                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " is not unique!!! ", "player", strSpeaker);
                                            return;
                                    }


                                }
                                else if (!GetAccountPrivileges(strSpeaker).CanKickPlayers && (ingamecommand[0].ToLower() == strKickCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to kick !!!", "player", strSpeaker);
                                    return;
                                }
                                #endregion

                                #region checkslycommand
                                if (GetAccountPrivileges(strSpeaker).CanKillPlayers && ingamecommand[0].ToLower() == strSlayCommand.ToLower())
                                {
                                    int playercount = 0;
                                    string SoldierName = String.Empty;
                                    string chatreason = String.Empty;

                                    for (int i = 2; i < elements; i++)
                                    {
                                        chatreason += SQLcleanup(ingamecommand[i].Trim()) + " ";
                                    }

                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        foreach (KeyValuePair<string, CPlayerInfo> player in FrostbitePlayerInfoList)
                                        {
                                            if (!String.IsNullOrEmpty(ingamecommand[1])) ingamecommand[1].Trim();
                                            if (player.Key.ToLower().Contains(ingamecommand[1].ToLower()))
                                            {
                                                SoldierName = player.Value.SoldierName;
                                                playercount++;
                                            }
                                        }
                                    }
                                    switch (playercount)
                                    {
                                        case 0:

                                            this.ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " not found!!!", "player", strSpeaker);
                                            return;

                                        case 1:

                                            ExecuteCommand("procon.protected.send", "admin.killPlayer", SoldierName);
                                            ExecuteCommand("procon.protected.send", "admin.say", SoldierName + " got killed for " + chatreason, "all");
                                            banningadmin = "";
                                            return;

                                        default:

                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " is not unique!!! " + chatreason, "player", strSpeaker);
                                            return;
                                    }


                                }
                                else if (!GetAccountPrivileges(strSpeaker).CanKillPlayers && (ingamecommand[0].ToLower() == strSlayCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to kill !!!", "player", strSpeaker);
                                    return;
                                }
                                #endregion

                                #region checktimebancommand
                                if (GetAccountPrivileges(strSpeaker).CanTemporaryBanPlayers && ingamecommand[0].ToLower() == strTimeBanCommand.ToLower())
                                {
                                    int playercount = 0;
                                    string EAGuid = String.Empty;
                                    string SoldierName = String.Empty;
                                    string chatreason = String.Empty;
                                    TimeoutSubset duration = new TimeoutSubset(TimeoutSubset.TimeoutSubsetType.Seconds, 3600);

                                    if (!String.IsNullOrEmpty(ingamecommand[1]))
                                    {
                                        foreach (KeyValuePair<string, CPlayerInfo> player in FrostbitePlayerInfoList)
                                        {
                                            if (player.Key.ToLower().Contains(ingamecommand[1].ToLower()))
                                            {
                                                EAGuid = player.Value.GUID;
                                                DebugInfo("full", "EAGuid" + EAGuid);
                                                SoldierName = player.Key;
                                                playercount++;
                                            }
                                        }
                                    }

                                    if (elements >= 3)
                                    {
                                        if (!String.IsNullOrEmpty(ingamecommand[2]))
                                        {
                                            string time = ingamecommand[2].Substring(0, ingamecommand[2].Length - 1);

                                            int bantime;

                                            #region check time specification
                                            if (ingamecommand[2].ToLower().EndsWith("w"))
                                            {
                                                if (!String.IsNullOrEmpty(time))
                                                {
                                                    if (int.TryParse(time, out bantime))
                                                    {
                                                        //604800 s == one week
                                                        duration.Seconds = bantime * 604800;
                                                        for (int i = 3; i < ingamecommand.Length; i++)
                                                        {
                                                            chatreason += ingamecommand[i].Trim();

                                                        }
                                                    }
                                                    else DebugInfo("full", "found w at the end but cant convert to UInt32");
                                                }
                                                else DebugInfo("full", "found w but string is empty now");
                                            }
                                            else
                                            {
                                                DebugInfo("full", "w not found");

                                                if (ingamecommand[2].ToLower().EndsWith("d"))
                                                {
                                                    if (!String.IsNullOrEmpty(time))
                                                    {
                                                        if (int.TryParse(time, out bantime))
                                                        {
                                                            duration.Seconds = bantime * 86400;
                                                            for (int i = 3; i < ingamecommand.Length; i++)
                                                            {
                                                                chatreason += ingamecommand[i].Trim();

                                                            }
                                                        }
                                                        else DebugInfo("full", "found d at the end but cant convert to UInt32");
                                                    }
                                                    else DebugInfo("full", "found d but string is empty now");
                                                }
                                                else
                                                {
                                                    DebugInfo("full", "d not found");

                                                    if (ingamecommand[2].ToLower().EndsWith("m"))
                                                    {
                                                        if (!String.IsNullOrEmpty(time))
                                                        {
                                                            if (int.TryParse(time, out bantime))
                                                            {
                                                                //604800 s == one week
                                                                duration.Seconds = bantime * 60;
                                                                for (int i = 3; i < ingamecommand.Length; i++)
                                                                {
                                                                    chatreason += ingamecommand[i].Trim();

                                                                }
                                                            }
                                                            else DebugInfo("full", "found m at the end but cant convert to Int32");
                                                        }
                                                        else DebugInfo("full", "found m but string is empty now");
                                                    }
                                                    else
                                                    {
                                                        DebugInfo("full", "m not found");

                                                        if (int.TryParse(time, out bantime))
                                                        {
                                                            duration.Seconds = bantime * 3600;
                                                        }
                                                        else
                                                        {
                                                            for (int i = 2; i < ingamecommand.Length; i++)
                                                            {
                                                                chatreason += ingamecommand[i].Trim();

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                    }

                                    CBanInfo victim = new CBanInfo("guid", EAGuid, duration, chatreason);
                                    switch (playercount)
                                    {
                                        case 0:

                                            this.ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " not found!!!", "player", strSpeaker);
                                            return;

                                        case 1:

                                            banningadmin = strSpeaker;

                                            if (!String.IsNullOrEmpty(victim.Guid))
                                            {

                                                if (AddBan(victim))
                                                {
                                                    TakeAction(SoldierName, chatreason, TimeoutSubset.TimeoutSubsetType.Seconds);
                                                    ExecuteCommand("procon.protected.send", "admin.say", SoldierName + " got banned for " + chatreason, "all");
                                                    banningadmin = "";
                                                    return;
                                                }
                                            }
                                            else
                                                ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " ERROR: EAGuid emtpy. Try again ...", "player", strSpeaker);
                                            return;

                                        default:

                                            ExecuteCommand("procon.protected.send", "admin.say", ingamecommand[1] + " is not unique!!! ", "player", strSpeaker);
                                            return;
                                    }
                                }
                                else if (!GetAccountPrivileges(strSpeaker).CanTemporaryBanPlayers && (ingamecommand[0].ToLower() == strTimeBanCommand.ToLower()))
                                {
                                    ExecuteCommand("procon.protected.send", "admin.say", "You have not enough privileges to timeban !!!", "player", strSpeaker);
                                    return;
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
        }

        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            // doesn't matter what kind of chat the message was typed
            OnGlobalChat(strSpeaker, strMessage);
        }

        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            OnGlobalChat(strSpeaker, strMessage);
        }

        public override void OnBanAdded(CBanInfo ban)
        {
            ScaledDebugInfo(1, "OnBanAdded fired");
            ScaledDebugInfo(3, ban.SoldierName + " " + ban.IdType + " " + ban.Guid + " " + ban.BanLength.Subset.ToString());
            bool isbanned = false;
            lock (Locker)
            {
                isbanned = AddBan(ban);

            }
            #region removeban
            if (isbanned && ebEmptyServerBanlist == enumBoolYesNo.Yes)
            {
                switch (ban.IdType)
                {
                    case "pbguid":
                        ExecuteCommand("procon.protected.tasks.add", ban.Guid, "0", "1", "1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_unbanguid " + ban.Guid);
                        break;
                    case "guid":
                        ExecuteCommand("procon.protected.tasks.add", ban.Guid, "0", "1", "1", "procon.protected.send", "banList.remove", ban.IdType, ban.Guid);
                        break;
                    case "name":
                        ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", ban.IdType, ban.SoldierName);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (ebEmptyServerBanlist == enumBoolYesNo.No)
                {

                }
            }
            #endregion
            this.ScaledDebugInfo(2, "OnBanAdded left");
        }

        public override void OnBanList(List<CBanInfo> banList)
        {
            this.ScaledDebugInfo(1, "OnBanList");
            this.ScaledDebugInfo(3, "Banlist with " + banList.Count + " entries...");

            lock (Locker)
            {
                if (ebEmptyServerBanlist == enumBoolYesNo.Yes)
                {

                    foreach (CBanInfo ban in banList)
                    {
                        this.ScaledDebugInfo(4, "OnBanList:: Guid: " + ban.Guid + " Name: " + ban.SoldierName + " IP: " + ban.IpAddress + " reason: " + ban.Reason + " BanType: " + ban.IdType);
                        if (AddBan(ban))
                        {
                            if (ban.IdType.ToLower() == "guid") ExecuteCommand("procon.protected.send", "banList.remove", "guid", ban.Guid);
                            if (ban.IdType.ToLower() == "name") ExecuteCommand("procon.protected.send", "banList.remove", "name", ban.SoldierName);
                        }
                    }
                    ExecuteCommand("procon.protected.send", "banList.save");
                }
            }
        }

        public override void OnLevelLoaded(String mapFileName, String Gamemode, int roundsPlayed, int roundsTotal)
        {
            ScaledDebugInfo(1, "OnLevelLoaded fire");
            lock (Locker)
            {
                if (OdbcCon == null)
                {
                    PluginConsoleWrite("ERROR in SQLBanRequest: OdbcConnection-Object is null");
                    PluginConsoleWrite("Try to connect to Database again ...");
                    OdbcCon = CheckDatabase();

                }
                if (OdbcCon.State != ConnectionState.Open)
                {
                    OdbcCon.Close();
                    OdbcCon.Open();
                }
                String sql = "UPDATE `" + strSQLDatabase + "`.`banlist` SET `expired` = 'y' WHERE `banlength` LIKE 'round';";
                DebugInfo("full", sql);

                using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                {
                    OdbcCom.ExecuteNonQuery();
                }
            }
        }


        public override void OnPunkbusterBanInfo(CBanInfo ban)
        {
            if (ebPBDisable == enumBoolYesNo.Yes) return;
            ScaledDebugInfo(1, "OnPunktbusterBanInfo fired");
            ScaledDebugInfo(4, ban.SoldierName + " " + ban.Guid + " " + ban.IdType + " " + ban.BanLength.Subset + " " + ban.BanLength.Seconds);
            ExecuteCommand("procon.protected.tasks.remove", "banempty");
            ExecuteCommand("procon.protected.tasks.remove", "banload");
            lock (Locker)
            {
                if (AddBan(ban) && ebEmptyServerPBBanlist == enumBoolYesNo.Yes)
                {
                    ExecuteCommand("procon.protected.tasks.add", ban.Guid, "0", "1", "1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_unbanguid " + ban.Guid);
                    ScaledDebugInfo(4, ban.SoldierName + " is now in Database and removed on local banlist");
                    ExecuteCommand("procon.protected.tasks.add", "banempty", "5", "1", "1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_banempty");
                    ExecuteCommand("procon.protected.tasks.add", "banload", "5", "1", "1", "procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_banload");

                }
                else
                {
                    if (ebEmptyServerPBBanlist == enumBoolYesNo.No) ScaledDebugInfo(4, ban.SoldierName + " is now in Database but not removed on local cause of Testmode");
                    else ScaledDebugInfo(4, ban.SoldierName + " is NOT added to Database and not deleted from local banlist");
                }
            }

        }

        #endregion

        #region other methods

        /// <summary>
        /// execute a kick
        /// </summary>
        /// <param name="name">name of the player to remove</param>
        /// <param name="reason">reason shown to the player</param>
        private void TakeAction(String name, String reason, TimeoutSubset.TimeoutSubsetType banlength)
        {
            if (this.ebTestrun == enumBoolYesNo.Yes)
            {
                this.DebugInfo("test", "  name" + name + " would have been kicked for " + reason);
            }
            else
            {
                switch (banlength)
                {
                    case TimeoutSubset.TimeoutSubsetType.Permanent:

                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", name, "YOUR ARE BANNED FOR: " + reason);
                        if (ebPrintKicksToConsole == enumBoolYesNo.Yes) PluginConsoleWrite("Kicked " + name + " for " + reason + " permanent");
                        return;

                    case TimeoutSubset.TimeoutSubsetType.Seconds:

                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", name, "YOUR ARE TIMEBANNED FOR: " + reason);
                        if (ebPrintKicksToConsole == enumBoolYesNo.Yes) PluginConsoleWrite("Kicked " + name + " for " + reason + " (time)");
                        return;
                    case TimeoutSubset.TimeoutSubsetType.Round:

                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", name, "YOUR ARE ROUNDBANNED FOR: " + reason);
                        if (ebPrintKicksToConsole == enumBoolYesNo.Yes) PluginConsoleWrite("Kicked " + name + " for " + reason + " (round)");
                        return;
                    default:
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", name, "YOUR ARE KICKED FOR: " + reason);
                        if (ebPrintKicksToConsole == enumBoolYesNo.Yes) PluginConsoleWrite("Kicked " + name + " for " + reason);
                        return;
                }
            }
        }

        /// <summary>
        /// check's whether the banlist-table already exists, creating it if not
        /// </summary>
        private OdbcConnection CheckDatabase()
        {
            // check database information
            lock (Locker)
            {
                OdbcConnection OdbcCon = null;
                if (!String.IsNullOrEmpty(strSQLHost) && !String.IsNullOrEmpty(strSQLPort) && !String.IsNullOrEmpty(strSQLDatabase) && !String.IsNullOrEmpty(strSQLUserName) && !String.IsNullOrEmpty(strSQLPassword))
                {
                    try
                    {
                        // for banlist
                        OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={" + strDatabaseDriver + "};" +
                                                                               "SERVER=" + this.strSQLHost + ";" +
                                                                               "PORT=" + this.strSQLPort + ";" +
                                                                               "DATABASE=" + this.strSQLDatabase + ";" +
                                                                               "User=" + this.strSQLUserName + ";" +
                                                                               "Password=" + this.strSQLPassword + ";" +
                                                                               "OPTION=3;");
                        OdbcCon.Open();

                        if (OdbcCon.State == ConnectionState.Open)
                        {
                            string sql = "CREATE TABLE IF NOT EXISTS `" + this.strSQLDatabase + @"`.`banlist` (
                                    `id` INT( 11 ) NOT NULL AUTO_INCREMENT,
                                    `ClanTag` VARCHAR( 10 ) DEFAULT NULL,
									`SoldierName` VARCHAR( 50 ) DEFAULT NULL,
                                    `EAGuid` VARCHAR( 35 ) DEFAULT NULL,
                                    `PBGuid` VARCHAR( 32 ) DEFAULT NULL,
									`IP` VARCHAR(15) DEFAULT NULL,
                                    `reason` VARCHAR( 150 ) NOT NULL DEFAULT '-Banned using banlist-',
                                    `banning_admin` VARCHAR( 50 ) NOT NULL DEFAULT '-Unknown-',
                                    `bantype` VARCHAR( 10 ) NOT NULL DEFAULT 'guid',
                                    `banlength` ENUM( 'perm', 'time', 'round') NOT NULL DEFAULT 'perm',
                                    `banduration` TIMESTAMP NOT NULL DEFAULT '0000-00-00 00:00:00',
                                    `expired` VARCHAR( 1 ) NOT NULL DEFAULT 'n',
                                    `servergroup` INT NOT NULL DEFAULT '0',
                                    `servername` VARCHAR( 150 ) DEFAULT NULL,
                                    `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    `countrycode` VARCHAR(2) DEFAULT NULL,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";

                            using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                            {
                                OdbcCom.ExecuteNonQuery();

                            }

                            sql = "CREATE TABLE IF NOT EXISTS `" + this.strSQLDatabase + @"`.`ServerGroup` (
                                    `id` INT( 11 ) NOT NULL AUTO_INCREMENT,
                                    `servergroup` INT NOT NULL DEFAULT '0',
                                    `servername` VARCHAR( 150 ) DEFAULT NULL,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
                            using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                            {
                                OdbcCom.ExecuteNonQuery();

                            }
                            sql = "SELECT * FROM `" + this.strSQLDatabase + "`.`ServerGroup` WHERE servername LIKE '" + csiServerInfo.ServerName + "';";
                            using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                            {
                                DataTable dtData = new DataTable();

                                using (OdbcDataAdapter OdbcAdapter = new OdbcDataAdapter(OdbcCom))
                                {
                                    OdbcAdapter.Fill(dtData);
                                }
                                DebugInfo("full", dtData.Rows.Count.ToString());
                                if (dtData.Rows.Count != 0)
                                {
                                    sql = "UPDATE `" + strSQLDatabase + "`.`ServerGroup` SET servergroup = " + servergroup + " WHERE servername LIKE '" + csiServerInfo.ServerName + "';";
                                    DebugInfo("full", sql);
                                }

                                else
                                {
                                    sql = "INSERT INTO `" + this.strSQLDatabase + @"`.`ServerGroup` (servergroup, servername) VALUES ('" + servergroup + "', '" + csiServerInfo.ServerName + "');";
                                    DebugInfo("full", sql);
                                }
                            }
                            using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                            {
                                OdbcCom.ExecuteNonQuery();
                            }
                            return OdbcCon;
                        }
                        else
                        {
                            this.PluginConsoleWrite("OdbcConnection could not be opened at OnPluginEnable!");
                        }
                    }
                    catch (Exception e)
                    {
                        this.PluginConsoleWrite("Exception while CheckDatabase: " + e.ToString());
                        OdbcCon.Close();
                        return null;
                    }
                }
                else
                {
                    this.PluginConsoleWrite("Please enter all database-details!");
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// sql query if player or guid or pbguid is banned 
        /// </summary>
        /// <param name="SoldierName"></param>
        /// <param name="Guid"></param>
        /// <param name="PBGuid"></param>
        /// <returns>
        /// DaTaTable of the Request or Null
        /// </returns>
        private DataTable SQLBanRequest(string SoldierName, string Guid, string PBGuid)
        {
            ScaledDebugInfo(1, "SQLBanRequest entered");

            if (OdbcCon == null)
            {
                PluginConsoleWrite("ERROR in SQLBanRequest: OdbcConnection-Objekt is null");
                PluginConsoleWrite("Try to connect to Database again ...");
                OdbcCon = CheckDatabase();

                return null;
            }
            if (OdbcCon.State != ConnectionState.Open)
            {
                OdbcCon.Close();
                OdbcCon.Open();
            }

            #region checktimeban

            String sql = "UPDATE `" + strSQLDatabase + @"`.`banlist` SET `expired` = 'y' WHERE `banlength` LIKE 'time' 
			AND (`banduration` <= '" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "');";

            DebugInfo("full", sql);
            using (OdbcCommand OdbcCommand = new OdbcCommand(sql, OdbcCon))
            {
                using (OdbcCommand OdbcCom = new OdbcCommand(sql, OdbcCon))
                {
                    OdbcCom.ExecuteNonQuery();
                }
            }
            #endregion

            sql = "SELECT * FROM `" + this.strSQLDatabase + "`.`banlist` WHERE (";
            bool sqlender = false;
            if (!String.IsNullOrEmpty(SoldierName))
            {
                sql += "SoldierName LIKE '" + SoldierName + "'";
                sqlender = true;
            }
            if (!String.IsNullOrEmpty(Guid))
            {
                if (sqlender) sql += " OR EAGUID LIKE '" + Guid + "'";
                else
                {
                    sql += "EAGUID LIKE '" + Guid + "'";
                    sqlender = true;
                }
            }
            if (!String.IsNullOrEmpty(PBGuid))
            {
                if (sqlender) sql += " OR PBGUID LIKE '" + PBGuid + "'";
                else
                {
                    sql += "PBGUID LIKE '" + PBGuid + "'";
                    sqlender = true;
                }
            }
            sql += ") AND expired LIKE 'n' AND (servergroup LIKE '" + this.servergroup + "' OR servergroup LIKE '0');";
            this.DebugInfo("full", "SQLRequest");
            this.DebugInfo("full", sql);

            using (OdbcCommand OdbcCommand = new OdbcCommand(sql, OdbcCon))
            {
                DataTable dtData = new DataTable();

                using (OdbcDataAdapter OdbcAdapter = new OdbcDataAdapter(OdbcCommand))
                {
                    OdbcAdapter.Fill(dtData);
                }
                if (dtData != null)
                {
                    DebugInfo("full", "SQLRequest: dtData not null");
                    ScaledDebugInfo(2, "SQLBanRequest left (dtData != null)");
                    return dtData;
                }

                else
                {
                    DebugInfo("full", "SQLRequest: dtData is null");
                    ScaledDebugInfo(2, "SQLBanRequest left (dtData == null)");
                    return null;
                }
            }
        }

        /// <summary>
        /// checks if a player is banned
        /// if banned it forces a kick and returns the Banreason else Banreason is null
        /// </summary>
        /// <param name="CPlayerInfo Object"></param>
        /// <returns>BANREASON</returns>
        private string CheckPlayerBanned(string SoldierName, string Guid, string PBGuid)
        {
            ScaledDebugInfo(1, "CheckPlayerBanned entered");
            DebugInfo("full", "Soldier: " + SoldierName + " Guid: " + Guid + " PBGuid: " + PBGuid);
            DataTable dtData = SQLBanRequest(SoldierName, Guid, PBGuid);
            if (dtData != null)
            {
                if (dtData.Rows.Count > 0)
                {
                    if (dtData.Rows[0]["banlength"].ToString() == "time")
                    {

                        TimeSpan diff = (DateTime.Parse(dtData.Rows[0]["banduration"].ToString()) - DateTime.UtcNow);
                        TakeAction(SoldierName, "(" + diff.Days + " d " + diff.Hours + " h " + diff.Minutes + " m) " + dtData.Rows[0]["reason"].ToString(), TimeoutSubset.TimeoutSubsetType.Seconds);
                        return "(" + diff.Days + " d " + diff.Hours + " h " + diff.Minutes + " m) " + dtData.Rows[0]["reason"].ToString();
                    }
                    if (dtData.Rows[0]["banlength"].ToString() == "round")
                    {

                        TakeAction(SoldierName, dtData.Rows[0]["reason"].ToString(), TimeoutSubset.TimeoutSubsetType.Round);
                        return dtData.Rows[0]["reason"].ToString();
                    }
                    if (dtData.Rows[0]["banlength"].ToString() == "perm")
                    {

                        TakeAction(SoldierName, dtData.Rows[0]["reason"].ToString(), TimeoutSubset.TimeoutSubsetType.Permanent);
                        return dtData.Rows[0]["reason"].ToString();
                    }
                    return dtData.Rows[0]["reason"].ToString();
                }
                DebugInfo("full", "CheckPlayerbanned dtData.Rows.Count: " + dtData.Rows.Count.ToString());
                return null;
            }
            DebugInfo("full", "CheckPlayerBanned: dtData is null");
            return "error";
        }

        /// <summary>
        /// only add ban to banlist if it doesn't exist
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if ban successful added or already exist else false</returns>
        private Boolean AddBan(CBanInfo player)
        {
            this.ScaledDebugInfo(1, "AddBan entered");

            #region CheckConditions
            if (player.IdType.ToLower() == "name" && String.IsNullOrEmpty(player.SoldierName))
            {
                PluginConsoleWrite("ERROR: could not force ban caus Bantype is NAME but name is null or empty");
                return false;
            }
            if (player.IdType.ToLower() == "guid" && String.IsNullOrEmpty(player.Guid))
            {
                PluginConsoleWrite("ERROR: could not force ban caus Bantype is EAGUID but EAGUID is null or empty");
                return false;
            }
            if (player.IdType.ToLower() == "pbguid" && String.IsNullOrEmpty(player.SoldierName))
            {
                PluginConsoleWrite("ERROR: could not force ban caus Bantype is PBGUID but PBGUID is null or empty");
                return false;
            }
            if (player.IdType.ToLower() == "ip")
            {
                PluginConsoleWrite("ERROR: could not force ban caus Bantype is IP");
                return false;
            }
            if (String.IsNullOrEmpty(player.IdType.ToLower()))
            {
                PluginConsoleWrite("ERROR: could not force ban caus Bantype is null or empty");
                return false;
            }
            this.ScaledDebugInfo(3, "Passed all Checks in AddBan");
            #endregion CheckConditions

            string reason = String.Empty;
            string SoldierName = String.Empty;
            string guid = String.Empty;
            string pbguid = String.Empty;
            string countrycode = String.Empty;
            string clantag = String.Empty;
            switch (player.IdType)
            {
                case "name":
                    SoldierName = player.SoldierName;
                    if (PunkbusterPlayerInfoList.ContainsKey(SoldierName))
                    {
                        pbguid = PunkbusterPlayerInfoList[player.SoldierName].GUID;
                        countrycode = PunkbusterPlayerInfoList[player.SoldierName].PlayerCountryCode;
                    }
                    if (FrostbitePlayerInfoList.ContainsKey(SoldierName))
                    {
                        guid = FrostbitePlayerInfoList[SoldierName].GUID;
                        clantag = FrostbitePlayerInfoList[SoldierName].ClanTag;
                    }
                    reason = CheckPlayerBanned(player.SoldierName, guid, pbguid);
                    break;
                case "guid":
                    guid = player.Guid;
                    if (String.IsNullOrEmpty(player.SoldierName))
                    {
                        foreach (KeyValuePair<string, CPlayerInfo> soldier in FrostbitePlayerInfoList)
                        {
                            if (soldier.Value.GUID.ToLower() == guid.ToLower())
                            {
                                if (!String.IsNullOrEmpty(soldier.Key)) SoldierName = soldier.Key;
                                if (!String.IsNullOrEmpty(soldier.Value.ClanTag)) clantag = soldier.Value.ClanTag;
                                break;
                            }
                        }
                        if (PunkbusterPlayerInfoList.ContainsKey(SoldierName))
                        {
                            if (!String.IsNullOrEmpty(PunkbusterPlayerInfoList[SoldierName].GUID)) pbguid = PunkbusterPlayerInfoList[SoldierName].GUID;
                            if (!String.IsNullOrEmpty(PunkbusterPlayerInfoList[SoldierName].PlayerCountryCode)) countrycode = PunkbusterPlayerInfoList[SoldierName].PlayerCountryCode;
                        }
                    }
                    else
                    {
                        SoldierName = player.SoldierName;
                        if (PunkbusterPlayerInfoList.ContainsKey(SoldierName))
                        {
                            pbguid = PunkbusterPlayerInfoList[SoldierName].GUID;
                            countrycode = PunkbusterPlayerInfoList[SoldierName].PlayerCountryCode;
                        }
                        if (FrostbitePlayerInfoList.ContainsKey(SoldierName))
                        {
                            clantag = FrostbitePlayerInfoList[SoldierName].ClanTag;
                        }
                    }
                    reason = CheckPlayerBanned(SoldierName, guid, pbguid);
                    break;
                case "pbguid":
                    pbguid = player.Guid;
                    if (String.IsNullOrEmpty(player.SoldierName))
                    {
                        foreach (KeyValuePair<string, CPunkbusterInfo> soldier in PunkbusterPlayerInfoList)
                        {
                            if (soldier.Value.GUID.ToLower() == guid.ToLower())
                            {
                                SoldierName = soldier.Key;
                                countrycode = soldier.Value.PlayerCountryCode;
                                if (FrostbitePlayerInfoList.ContainsKey(SoldierName))
                                {
                                    guid = FrostbitePlayerInfoList[player.SoldierName].GUID;
                                    clantag = FrostbitePlayerInfoList[player.SoldierName].ClanTag;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        SoldierName = player.SoldierName;
                        if (PunkbusterPlayerInfoList.ContainsKey(SoldierName)) countrycode = PunkbusterPlayerInfoList[SoldierName].PlayerCountryCode;
                        if (FrostbitePlayerInfoList.ContainsKey(SoldierName))
                        {
                            guid = FrostbitePlayerInfoList[player.SoldierName].GUID;
                            clantag = FrostbitePlayerInfoList[player.SoldierName].ClanTag;
                        }
                        else guid = "";
                    }
                    reason = CheckPlayerBanned(SoldierName, guid, pbguid);
                    break;
                default:
                    SoldierName = String.Empty;
                    guid = String.Empty;
                    pbguid = String.Empty;
                    break;
            }
            if (reason == null)
            {
                ScaledDebugInfo(4, "Ban doesn't exist already, adding it...");
                String sql = ("INSERT INTO `" + this.strSQLDatabase + @"`.`banlist` (SoldierName, EAGUID, PBGUID, 
				reason, banning_admin, banduration, servername, servergroup, timestamp, bantype, banlength, countrycode, ClanTag) VALUES ('");
                sql += SoldierName + "', '" + guid + "', '" + pbguid + "', '";
                if (!String.IsNullOrEmpty(player.Reason))
                {
                    reason = player.Reason;
                    reason.Trim();
                    DebugInfo("full", reason);
                    Regex.Replace(reason, "BC2!", "");
                    Regex.Replace(reason, "[Admin Decision]", "");
                    DebugInfo("full", reason);
                    sql += (SQLcleanup(reason) + "', '");
                }
                else sql += "', '";
                sql += (banningadmin + "', '");
                if (player.BanLength.Subset == TimeoutSubset.TimeoutSubsetType.Seconds) sql += (DateTime.UtcNow.AddSeconds(player.BanLength.Seconds).ToString("yyyy-MM-dd HH:mm:ss") + "', '");
                else sql += "', '";
                if (!string.IsNullOrEmpty(csiServerInfo.ServerName)) sql += (csiServerInfo.ServerName + "', '");
                else sql += "', '";
                sql += (servergroup + "', '");
                sql += (DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "', '");
                sql += (player.IdType.ToLower() + "', '");
                switch (player.BanLength.Subset)
                {
                    case TimeoutSubset.TimeoutSubsetType.Permanent:
                        sql += ("perm', '");
                        break;
                    case TimeoutSubset.TimeoutSubsetType.Seconds:
                        if (player.BanLength.Seconds <= 0) return false;
                        PluginConsoleWrite("ERROR: could not force ban because BanLengthtype is time but time <= 0");
                        sql += ("time', '");
                        break;
                    case TimeoutSubset.TimeoutSubsetType.Round:
                        sql += ("round', '");
                        break;
                    default:
                        PluginConsoleWrite("ERROR: could not force ban because BanLengthtype is set to none");
                        return false;
                }
                sql += (countrycode + "', '");
                sql += (clantag + "');");
                DebugInfo("full", sql);
                using (OdbcCommand OdbcCommand2 = new OdbcCommand(sql, OdbcCon))
                {
                    OdbcCommand2.ExecuteNonQuery();

                }
                return true;
            }
            else
            {
                if (reason == "error")
                {
                    this.DebugInfo("full", "error");
                    return false;
                }

                this.DebugInfo("full", "Ban already existed!");
                return true;
            }
        }

        /// <summary>
        /// remove a ban from banlist
        /// </summary>
        /// <param name="SoldierName"></param>
        /// <returns>string if unban was successful or not</returns>
        private string RemoveBan(string SoldierName)
        {
            this.ScaledDebugInfo(1, "RemoveBan entered");
            DataTable dtData = SQLBanRequest(SoldierName, "", "");
            if (dtData.Rows == null || dtData.Rows.Count == 0)
            {
                return (SoldierName + " is not banned on " + this.csiServerInfo.ServerName);
            }
            else
            {
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    String sql = "UPDATE `" + strSQLDatabase + "`.`banlist` SET `expired` = 'y' WHERE `banlist`.`id` = '" + dtData.Rows[i]["id"].ToString() + "'";
                    DebugInfo("full", sql);
                    using (OdbcCommand OdbcCommand2 = new OdbcCommand(sql, OdbcCon))
                    {
                        OdbcCommand2.ExecuteNonQuery();
                    }
                }
                return (SoldierName + " unbanned");
            }
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

        /// <summary>
        /// write a message to the plugin-console
        /// </summary>
        /// <param name="message">message to display</param>
        private void PluginConsoleWrite(String message)
        {
            String line = String.Format("^b^8BanManager^0:^n {0}", message);
            this.ExecuteCommand("procon.protected.pluginconsole.write", line);
        }

        /// <summary>
        /// write an ingame-message to all players
        /// </summary>
        /// <param name="message">message to display</param>
        /*private void IngameSayAll(String message)
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
        }*/

        /// <summary>
        /// write an ingame-message to a specific squad 
        /// </summary>
        /// <param name="message">message to displayer</param>
        /// <param name="teamid">ID of the desired team</param>
        /// <param name="squadid">ID of the desired squad</param>
        private string SQLcleanup(String SQLmessage)

        {
            if (String.IsNullOrEmpty(SQLmessage)) return "";
            this.ScaledDebugInfo(6, "Ban reason Cleanup - Entered");

            // Replace invalid characters with empty strings.
            SQLmessage.Trim();
            return Regex.Replace(SQLmessage, "['\\/]", "");
        }

        /// <summary>
        /// write an ingame-message to a specific squad 
        /// </summary>
        /// <param name="message">message to displayer</param>
        /// <param name="teamid">ID of the desired team</param>
        /// <param name="squadid">ID of the desired squad</param>
        /*private void IngameSaySquad(String message, int teamid, int squadid)
        {
            List<String> wordWrappedLines = this.WordWrap(message, 100);
            foreach (String line in wordWrappedLines)
            {
                String formattedLine = String.Format("[RemoteBanlist] {0}", line);
                this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "squad", teamid.ToString(), squadid.ToString());
            }
        }*/

        public bool IsNumeric(System.Object Expression)
        {
            if (Expression == null || Expression is DateTime)
                return false;

            if (Expression is Int16 || Expression is Int32 || Expression is Int64 || Expression is Decimal || Expression is Single || Expression is Double || Expression is Boolean)
                return true;

            try
            {
                if (Expression is string)
                    Double.Parse(Expression as string);
                else
                    Double.Parse(Expression.ToString());
                return true;
            }
            catch { } // just dismiss errors but return false
            return false;
        }

        #endregion

        #region own classes & enums

        /// <summary>
        /// enumeration for setting the type of a ban
        /// </summary>
        public enum BanType { Name, EAGUID, PBGUID }

        /// <summary>
        /// holds additional information about a ban, designed to be stored in a MySQL-database
        /// </summary>
        public class CRemoteBanInfo
        {
            /// <summary>
            /// create a new RemoteBanInfo-object and initialise all variables with empty values
            /// </summary>
            public CRemoteBanInfo()
            {
                this.SoldierName = null;
                this.EAGUID = null;
                this.PBGUID = null;
                this.Reason = null;
                this.BanningAdmin = null;
                this.Type = BanType.Name;
                this.Length = TimeoutSubset.TimeoutSubsetType.Permanent;
                this.Duration = DateTime.UtcNow;
                this.TimeStamp = DateTime.UtcNow;
            }
            public String SoldierName { get { return this.SoldierName; } set { this.SoldierName = value; } }
            /// <summary>
            /// EA GUID of the banned player
            /// </summary>
            public String EAGUID { get { return this.EAGUID; } set { this.EAGUID = value; } }
            /// <summary>
            /// PB GUID of the banned player
            /// </summary>
            public String PBGUID { get { return this.PBGUID; } set { this.PBGUID = value; } }
            /// <summary>
            /// reason for banning
            /// </summary>
            public String Reason { get { return this.Reason; } set { this.Reason = value; } }
            /// <summary>
            /// name of the admin issueing the ban
            /// </summary>
            public String BanningAdmin { get { return this.BanningAdmin; } set { this.BanningAdmin = value; } }
            /// <summary>
            /// type of the ban
            /// </summary>
            public BanType Type { get { return this.Type; } set { this.Type = value; } }
            /// <summary>
            /// length of the ban
            /// </summary>
            public TimeoutSubset.TimeoutSubsetType Length { get { return this.Length; } set { this.Length = value; } }
            /// <summary>
            /// duration of the ban (empty, seconds or timestamp)
            /// </summary>
            public DateTime Duration { get { return this.Duration; } set { this.Duration = value; } }
            /// <summary>
            /// timestamp of the ban-creation
            /// </summary>
            public DateTime TimeStamp { get { return this.TimeStamp; } set { this.TimeStamp = value; } }
        }
        #endregion
    }
}
