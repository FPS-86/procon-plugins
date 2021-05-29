/*  Copyright 2011 MorpheusX(AUT)

    http://www.morpheusx.at

    This file is part of MorpheusX(AUT)'s Plugins for BFBC2 PRoCon.

    MorpheusX(AUT)'s Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported
    (CC BY-NC-SA 3.0) License.

    MorpheusX(AUT)'s Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    Creative Commons License for more details.

    You should have received a copy of the CreativeCommons License
    along with MorpheusX(AUT)'s Plugins for BFBC2 PRoCon.  If not, see <http://creativecommons.org/licenses/>.
*/

/*
 * TO-DO List:
 * fix Suspectedness-algorithm (think about a better status-system :-D)
 * clean up unneeded stuff (spam console/debug mode, some doubled-checks)
 * optimize stored player data (guids, names, ids)
 * implement email notification feature
 * improve Blacklisting system (custom messages, kick/ban)
 * add check for player's global stats-last update
 * jumping one Suspectedness-status if another one is too suspicious
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Timers;
using System.Text;
using System.Security.Cryptography;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CheaterAlert : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables & Constructor

        // Server & Plugin-Info
        private string m_strServerIP;           // IP of the gameserver
        private string m_strPort;               // gameserver's port
        private string m_strServerAddress;      // combination of IP and port
        private string m_strPRoConVersion;      // PRoCon-Version running
        private bool m_IsPluginEnabled;         // states whether the plugin is enabled or not
        private string m_strServerName;         // ingame-name of the server
        private CServerInfo m_csiServerInfo;    // information about the server

        // Hashtables, Dictionaries & Lists to store data
        private Dictionary<string, CheaterAlert.PlayerStats> m_dicPlayerData = new Dictionary<string, CheaterAlert.PlayerStats>();  // dictionary storing nearly all data about the players
        private Dictionary<string, string> m_dicPlayerGUID = new Dictionary<string, string>();                                      // dictionary storing the players BC2 GUID (might get merged into m_dicPlayerData)
        private Dictionary<string, string> m_dicPlayerPBGUID = new Dictionary<string, string>();                                    // dictionary storing the players PB GUID (might get merged into m_dicPlayerData)
        private Dictionary<string, CPlayerInfo> m_dicPlayerFrostbiteInfo = new Dictionary<string, CPlayerInfo>();                   // dictionary storing BC2 information about the players
        private Dictionary<string, CPunkbusterInfo> m_dicPlayerPunkbusterInfo = new Dictionary<string, CPunkbusterInfo>();          // dictionary storing PB information about the players
        private Dictionary<int, string> m_dicDetectedPlayers = new Dictionary<int, string>();                                         // dictionary storing all players, which have been detected by the plugin
        private Dictionary<string, int> m_dicKillsStart = new Dictionary<string, int>();                                            // dictionary storing the kills at the beginning of the KPM-calculation
        private List<string> m_lstClanBlackList;                                                                                    // blacklist of clans. soldiers wearing clantags on this list will be kicked/banned immediatelly
        private List<string> m_lstServerValues;                                                                                     // list of servervalue messages (might get replaced by a more efficient method)
        private List<CPlayerInfo> m_lstPlayerList;                                                                                  // list of all players on the server

        // Cheater-detection variables
        private int m_iPlayerCount;                 // number of players on the server
        private int m_iTotalKills;                  // total number of kills
        private string m_strCurrentMap;             // "converted" mapname
        private float m_fAverageKills;              // average number of kills
        private float m_fAverageDeaths;             // average number of deaths
        private float m_fAverageKDR;                // average KDR
        private float m_fAverageHSPercent;          // average headshot percentage
        private float m_fAverageKPM;                // average kills per minute
        private float m_iHighestKills;              // highest number of kills
        private float m_iHighestDeaths;             // highest number of deaths
        private float m_fHighestKDR;                // highest KDR
        private float m_fHighestHSPercent;          // highest headshot percentage
        private float m_fHighestKPM;                // highest kills per minute
        private int m_iIngameMaxKills;              // maximum ingame number of kills which a player can reach before getting suspicious
        private double m_dIngameMaxKDR;             // maximum ingame KDR which a player can reach before getting suspicious
        private double m_dIngameMaxHSPercent;       // maximum ingame headshot percentage which a player can reach before getting suspicious
        private double m_dIngameMaxKPM;             // maximum ingame kills per minute which a player can reach before getting suspicious
        private double m_dGlobalMaxKDR;             // maximum global KDR which a player can reach before getting suspicious
        private int m_iNumberOfDetectedPlayers;     // number of players detected by the plugin
        private bool m_blSendingPlayers;            // toggles whether the plugin is sending stats of detected players
        private bool m_blMinuteOver;                // toggles whether the minute for the KPM-calculation is over
        private bool m_blKPMRunning;                // states whether the KPM-calculation is already running

        // General variables
        private enumBoolYesNo m_DescriptionRead;        // makes sure the admin has read the plugin's description
        private enumBoolYesNo m_RemovePlayer;           // toggles whether detected players will be removed (= at least kicked) automatically
        private string m_strRemoveOption;               // toggles the way of removing players
        private string m_strBanType;                    // toggles the type of a ban
        private enumBoolYesNo m_PBGUIDBan;              // toggles whether the EA or PB GUID should be used
        private string m_strKickReason;                 // reason displayed to a player when he gets kicked by the plugin
        private int m_iBanTime;                         // ban time in minutes
        private string m_strBanReason;                  // reason displayed to a player when he gets (temporarely) banned by the plugin
        private string m_strPermBanReason;              // reason displayed to a player when he gets permanently banned by the plugin
        private enumBoolYesNo m_AlterVariables;         // toggles whether an admin wants to change the preset cheater-detection values
        private enumBoolYesNo m_StreamToDatabase;       // toggles whether the plugin will send data to a central database (will be required in a future version)
        private enumBoolYesNo m_AutomaticCleanup;       // toggles whether the plugin will delete the stored data from time to time
        private string m_CleanupSpeed;                  // toggles the cleanup speed
        private int m_iCleanupAfterRounds;              // "translates" the chosen cleanup speed into a number of rounds
        private int m_iCleanupCurrentRounds;            // round-counter needed for automatic cleanup
        private enumBoolYesNo m_BlackListing;           // toggles whether the Blacklisting feature is enabled

        // Notification variables
        private enumBoolYesNo m_SpamConsole;            // toggles whether messages should be displayed within the plugin console
        private enumBoolYesNo m_DebugMode;              // toggles whether debug messages will be shown within the plugin console
        private enumBoolYesNo m_ServerValues;           // toggles whether server values will be shown within the plugin console
        private enumBoolYesNo m_ShowSysTrayAlert;       // toggles whether SysTrayAlerts should be shown
        private enumBoolYesNo m_IngameNotifications;    // toggles whether ingame-messages should be displayed when a cheater is detected
        private enumBoolYesNo m_LogData;                // toggles whether the plugin will create logfiles on the maschine it is running on
        private string m_strLogFilePathValues;          // path for the servervalues-logfiles
        private string m_strLogFilePathPlayers;         // path for the detectedplayers-logfiles
        private int m_iLogTime;                         // log-time in minutes

        // SQL variables
        private string m_strSQLHostname;                    // hostname of the mySQL-database server
        private string m_strSQLDatabaseName;                // name of the desired mySQL-database
        private string m_strSQLUsername;                    // username used to connect to the mySQL-database server
        private string m_strSQLPassword;                    // password used to connect to the mySQL-database server
        private System.Data.Odbc.OdbcCommand OdbcCom;       // ODBC-command 1
        private System.Data.Odbc.OdbcCommand OdbcComm;      // ODBC-command 2
        private System.Data.Odbc.OdbcConnection OdbcCon;    // ODBC-connection 1
        private System.Data.Odbc.OdbcConnection OdbcConn;   // ODBC-connection 2
        private System.Data.Odbc.OdbcDataReader OdbcDR;     // ODBC-reader
        private bool m_blOdbcOpen;                          // states whether the ODBC-connection 1 is established
        private bool m_blOdbc2Open;                         // states whether the ODBC-connection 2 is established

        // PBBans MBI variables
        private enumBoolYesNo m_RetrieveMBI;        // toggles whether the MBI should be retrieved
        private CDownloadFile m_cdfMBIDownloader;   // downloader for PBBans MBI
        private volatile bool m_blMBIDownload;      // toggles whether a download is running
        private volatile bool m_blMBISearch;        // toggles whether a search is running

        public CheaterAlert()
        {
            this.m_lstPlayerList = new List<CPlayerInfo>();
            this.m_lstClanBlackList = new List<string>();
            this.m_lstClanBlackList.Add("aa.net");

            this.m_DescriptionRead = enumBoolYesNo.No;
            this.m_ShowSysTrayAlert = enumBoolYesNo.No;
            this.m_SpamConsole = enumBoolYesNo.Yes;
            this.m_RemovePlayer = enumBoolYesNo.No;
            this.m_IngameNotifications = enumBoolYesNo.Yes;
            this.m_AutomaticCleanup = enumBoolYesNo.Yes;
            this.m_DebugMode = enumBoolYesNo.Yes;
            this.m_ServerValues = enumBoolYesNo.No;
            this.m_LogData = enumBoolYesNo.No;
            this.m_BlackListing = enumBoolYesNo.No;
            this.m_RetrieveMBI = enumBoolYesNo.No;

            this.m_AlterVariables = enumBoolYesNo.No;
            this.m_iIngameMaxKills = 50;
            this.m_dIngameMaxKDR = 6.0;
            this.m_dIngameMaxHSPercent = 75;
            this.m_dIngameMaxKPM = 2.0;
            this.m_dGlobalMaxKDR = 3.5;

            this.m_iBanTime = 60;
            this.m_iCleanupAfterRounds = 6;
            this.m_iLogTime = 5;
            this.m_iNumberOfDetectedPlayers = 0;

            this.m_strRemoveOption = "Kick";
            this.m_strBanType = "Name";
            this.m_PBGUIDBan = enumBoolYesNo.No;
            this.m_strKickReason = "[CheaterAlert] You are suspected of cheating and thus got kicked!";
            this.m_strBanReason = "[CheaterAlert] You are suspected of cheating and thus got banned for %bt% minutes!";
            this.m_strPermBanReason = "[CheaterAlert] You are suspected of cheating and thus got banned permanently!";
            this.m_CleanupSpeed = "Normal";

            this.m_strSQLHostname = "morpheusx.at";
            this.m_strSQLDatabaseName = "cheateralert";
            this.m_strSQLUsername = "cheateralert";
            this.m_strSQLPassword = "d!GH_/]Ql4m3yzx0=y[n/{,@Ry}§s0$P7üHQW{LjJ$Dö8h8§4z7eGjWwRm[cF!mR";

            this.m_blKPMRunning = false;
            this.m_blSendingPlayers = false;
            this.m_blMBIDownload = false;
            this.m_blMBISearch = false;
        }

        #endregion

        #region Plugin Setup

        public string GetPluginName()
        {
            return "Cheater Alert";
        }

        public string GetPluginVersion()
        {
            return "0.0.3.1";
        }

        public string GetPluginAuthor()
        {
            return "MorpheusX(AUT)";
        }

        public string GetPluginWebsite()
        {
            return "www.morpheusx.at/procon/cheateralert/";
        }

        public string GetPluginDescription()
        {
            return @"
<p>This Plugin was written by MorpheusX(AUT).<br>
<b>eMail:</b> procon(at)morpheusx(dot)at<br>
<b>Twitter:</b> <a href='http://twitter.com/#!/MorpheusXAUT'>MorpheusXAUT</a><br>
<b>BFcom.org:</b> <a href='http://www.bfcom.org/members/morpheusx-aut-.html'>MorpheusX(AUT)</a><br>
<b>phogue.net:</b> <a href='http://www.phogue.net/forumvb/member.php?565-MorpheusX%28AUT%29'>MorpheusX(AUT)</a><br>
If the links do not work for you, right-click them and select 'Copy Shortcut', then paste this URL in your browser. You can also find all links collected at 'http://www.morpheusx.at/procon/cheateralert/links.html'.<br><br></p>
<p align='center'>If you like my work, please consider donating!<br><br>
<form action='https://www.paypal.com/cgi-bin/webscr' method='post'>
<input type='hidden' name='cmd' value='_s-xclick'>
<input type='hidden' name='hosted_button_id' value='PLFJH26HK79AG'>
<input type='image' src='https://www.paypal.com/en_US/i/btn/btn_donate_LG.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
<img alt='' border='0' src='https://www.paypal.com/de_DE/i/scr/pixel.gif' width='1' height='1'>
</form>
<a href='https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=PLFJH26HK79AG'>Donation-Link</a></p>

<h2>Description</h2>
<p>Please be aware that the use of this plugin might break the <a href='http://forums.electronicarts.co.uk/battlefield-announcements/1167691-bfbc2-rules-conduct.html'>BFBC2 Rules of Conduct</a>.
Please ensure you have read and understood those rules before using the plugin! The plugin's author is not responsible for any actions done or harm taken by this plugin!<br><br>
<b>Cheater Alert</b> is an attempt to provide another cheat-detection system beside PunkBuster.<br>
This plugin uses some algorithms and simple mathematics to check players and filter out cheaters. Several stats - ingame as well as global ones - are compared to given values, and each player is ranked in a special security-system.<br>
Although the plugin uses several different security-levels, there is still the possibility to falsely detect a player, although this risk should be very small.<br><br>
If you find any bugs, mistakes or even crashes, please report immediatelly so the error can be fixed soon. You can either contact the author directely (see header of this description) or leave a post within the BFcom.org or phogue.net forums.<br>
If you want German support: <a href='http://www.bfcom.org/procon-support/5249-plugin-cheater-alert.html'>BFcom.org</a><br>
If you want English support: <a href='http://www.phogue.net/forumvb/showthread.php?1736'>phogue.net</a></p>

<h2>Setting up the mySQL-connection</h2>
<p>Since <b>Cheater Alert</b> keeps in touch with a mySQL-server to check your login data and exchange some information, you will have to install the <a href'http://dev.mysql.com/downloads/connector/odbc/'>ODBC-connector</a> on the machine the plugin is running on.<br>
blactionhero from phogue.net has created a tutorial how to install and configure this connector correctly. You can find this tutorial within the <a href'http://www.phogue.net/forumvb/showthread.php?1245-Howto-MySQL-Setup-and-Configuration.&p=9897&viewfull=1#post9897'>phogue.net forums</a>.<br>
Since all data is streamed to the same database, you can skip the server-setup part and just concentrate on the connector.<br>
The necessary connection-data is as follows:<br></p>
<ul>
<li><b>Data Source Name:</b> <i>the way you want to call this connection. 'cheateralert' is recommended.</i></li>
<li><b>Description:</b> <i>a short description for this connection.</i></li>
<li><b>TCP/IP-Server:</b> morpheusx.at</li>
<li><b>Port:</b> 3306</li>
<li><b>User:</b> cheateralert</li>
<li><b>Password:</b> d!GH_/]Ql4m3yzx0=y[n/{,@Ry}§s0$P7üHQW{LjJ$Dö8h8§4z7eGjWwRm[cF!mR</li>
<li><b>Database:</b> cheateralert</li>
</ul>
<p>Please make sure you copy every letter of the password. If you press 'Test', an error-message will most likely appear. You can ignore that, the connection is working anyways.<br>
Confirm all settings with 'OK' and your ODBC-configuration is done. Please note that this just has to be done once, but on every computer using this plugin.<br>
If you are using a Procon Layer Server hosted by any kind of provider, you must nag them to do those steps (setting up the connector on your local machine will NOT solve the problem, unless you keep your computer and Procon running all the time.)<br><br></p>

<h2>Configuring Procon's plugin settings</h2>
<p><b>Cheater Alert</b> needs to be able to connect to 2 hosts to do all work properly.<br>
Since connections created by plugins are normally blocked due to security reasons, you will have to alter some settings. Again, this has to be done on the machine running the plugin. If you are using a Procon Layer Server hosted by any kind of provider, you must nag them to do those steps (altering the plugin settings on your local machine will NOT solve the problem, unless you keep your computer and Procon running all the time.)<br><br></p>
<h3>Variant 1: disabling sandbox mode</h3>
<p>This variant is the easier one. You will have to disable Procon's sandbox-mode, which will remove the connection-blockades. This is easier to configure, but a little less secure.<br>
To do so please do the following:<br></p>
<ol>
<li>Click 'Tools' (in the upper right corner of your Procon-window</li>
<li>Click 'Options'</li>
<li>Click the 'Plugins' tab within the new window</li>
<li>Click the dropdown-list under 'Plugin security' and select 'Run plugins with no restrictions'</li>
<li>Close the Options-window by clicking 'Close' and restart your Procon</li>
</ol>
<h3>Variant 2: adding the necessary hosts to the exceptions</h3>
<p>This variant is the more secure, but also more 'difficult' one. It will require you to add the two hosts to the list of allowed hosts and furthermore allow ODBC-connections.<br>
To do so please do the following:<br></p>
<ol>
<li>Click 'Tools' (in the upper right corner of your Procon-window</li>
<li>Click 'Options'</li>
<li>Click the 'Plugins' tab within the new window</li>
<li>Click inside the 'Trusted host/domain' field and enter 'http://api.bfbcs.com'</li>
<li>Click inside the 'Port' field and enter '80'</li>
<li>Make sure the right line appears in the list below</li>
<li>Click the checkbox 'Allowed all outgoing ODBC connections' (has to be checked)</li>
<li>Close the Options-window by clicking 'Close' and restart your Procon</li>
</ol>

<h2>Setting up <b>Cheater Alert</b></h2>
<p>Once you have done above steps, you are ready to finally configure the plugin itself.<br>
All options will be explained in the following paragraph:<br></p>
<h3>1. General settings</h3>
<ul>
<li><b>I have read the description:</b> just a check to make sure you have read this ;-)</li>
<li><b>Clean up data automatically? (recommended):</b> toggles whether the plugin deletes all stored data every few rounds. This is recommended to prevent lag</li>
<li><b>Cleanup speed:</b> toggles the cleanup-speed</li>
<li><b>Stream to database? (recommended!):</b> Sends stats about kills, deaths, headshots, ... to the database. This is recommended to help improve the plugin</li>
<li><b>Scan PBBans' Master Ban Index?:</b> downloads the latest Master Ban Index from pbbans.com every 6 hours and compares the names of soldiers on it to players joining your server. Thus, non-streaming admins can have their server protected, plus cheaters, who transferred their soldier to a new GUID will be detected and removed</li>
</ul>
<h3>2. Notifications and logging</h3>
<ul>
<li><b>Show messages in the plugin-console:?</b> displays some messages about detected players and plugin changes within the plugin-console</li>
<li><b>Show debugmessages in the plugin-console?:</b> displays debugmessages (such as logging or calculation-activities) within the plugin-console</li>
<li><b>Show servervalues in the plugin-console?:</b> displays some averages + other values every 1 minute within the plugin-console</li>
<li><b>Show SysTray Alerts?:</b> displays a popup when your attention is required or a cheater is detected. Please not that if you are enabling this when using a Procon Layer Server, the popup will appear at your host's machine and not your local computer</li>
<li><b>Show ingame messages?:</b> displays an ingame-message to all players if a cheater is detected.</li>
<li><b>Log Data to file?:</b> toggles the logging-option of servervalues and detected players</li>
<li><b>Log interval (minutes):</b> duration between two servervalues-logentries</li>
</ul>
<h3>3. Cheat-detection Variables</h3>
<ul>
<li><b>Alter plugin variables (use with caution!):</b> toggles the ability to change the plugin's cheat-detection variables. Please be careful with this because the plugin hasn't been tested with other values and might not react as wished</li>
<li><b>Ingame max KDR:</b> if this value is exceeded, a player will be treated as suspicious concerning his ingame-KDR</li>
<li><b>Ingame max number of kills:</b> if this value is exceeded, a player will be treated as suspicious concerning his ingame-kills</li>
<li><b>Ingame max percentage of headshots:</b> if this value is exceeded, a player will be treated as suspicious concerning his ingame-headshots</li>
<li><b>Ingame max kills per minute:</b> if this value is exceeded, a player will be treated as suspicious concerning his ingame-KPM</li>
<li><b>Global max KDR:</b> if this value is exceeded, a player will be treated as suspicious concerning his global KDR</li>
</ul>
<h3>4. Remove Player</h3>
<ul>
<li><b>Enable removing feature?:</b> toggles the plugin's feature to remove detected players</li>
<li><b>Removing Option:</b> toggles the way of removing a player</li>
<li><b>Ban Type:</b> if temporary or permanent banning is enabled, the type of ban can be chosen</li>
<li><b>Use PunkBuster GUID instead of EA GUID?:</b> gives the opportunity to perform a PB-Ban. This is just available when 'Permanent Ban' is selected</li>
<li><b>Ban Time (minutes):</b> time in minutes, which a player will get banned</li>
<li><b>Kick Reason:</b> message displayed to a player when he is kicked</li>
<li><b>Ban Reason:</b> message displayed to a player when he is banned temporarely. Use %bt% to include the ban-time</li>
<li><b>Perm Ban Reason:</b> message displayed to a player when he is banned permanently</li>
</ul>
<h3>5. Blacklisting</h3>
<ul>
<li><b>Enable Blacklisting feature?:</b> toggles the plugin's blacklisting feature</li>
<li><b>Clan Black List:</b> exact ingame-clantag (case-insensitive) of players, who will get kicked immediatelly while wearing this tag</li>
</ul>

<h2>To-Do list</h2>
<ul>
<li>fix Suspectedness-algorithm (think about a better status-system :-D)</li>
<li>clean up unneeded stuff (spam console/debug mode, some doubled-checks)</li>
<li>optimize stored player data (guids, names, ids)</li>
<li>implement email notification feature</li>
<li>improve Blacklisting system (custom messages, kick/ban)</li>
<li>add check for player's global stats-last update</li>
<li>jumping one Suspectedness-status if another one is too suspicious</li>
</ul>

<h2>Known bugs/issues</h2>
<ul>
<li><b>Solved:</b> <span style='text-decoration:line-through'>Stats-fetching not working when the player name contains a '&'</span></li>
<li><b>Solved:</b> <span style='text-decoration:line-through'>Sending and logging of detected players didn't work</span></li>
<li><b>Solved:</b> <span style='text-decoration:line-through'>Cheat-detection algorithm not working properly (wasn't implemented correctly)</span></li>
<li><b>Solved:</b> <span style='text-decoration:line-through'>Removing-feature not working properly (wasn't implemented correctly)</span></li>
<li><b>Solved:</b> <span style='text-decoration:line-through'>White- and Blacklisting system doesn't work (plugin doesn't save entered data)</span></li>
</ul>

<h2>Special thanks</h2>
<p>There are pretty some people, who have helped me with this plugin in some kind. Please give me a shout if I missed someone.<br>
This list is in no specific order:<br></p>
<ul>
<li>phogue - for creating Procon and helping me out with programming a lot</li>
<li>XpKiller - for helping me much with ODBC-programming and SQL-stuff</li>
<li>Phil_K - for help with programming</li>
<li>Zaeed - for help with programming and some ideas</li>
<li>micovery - for help with programming and the great idea with the security-system</li>
<li>blactionhero - for help with programming and some ODBC- and SQL-help</li>
<li>DaBIGfisH - for a killrate-codesnippet</li>
<li>haclevan - for letting me test the plugin on his server and streaming stats</li>
<li>da_mike - for streaming stats</li>
<li>d1ApR1l - for creating BFBCS and the stats-API</li>
<li>all others, who gave me ideas and motivation to code this plugin</li>
</ul>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strServerIP = strHostName;
            this.m_strPort = strPort;
            this.m_strServerAddress = strHostName + ":" + strPort;
            this.m_strPRoConVersion = strPRoConVersion;

            StopKPM();
            this.m_lstServerValues = new List<string>();

            this.ConsoleWrite("Your humble servant is at your service! My current settings are:");
        }

        public void OnPluginEnable()
        {
            // just keeps track of some of the events fired to prevent lag
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnServerInfo", "OnPlayerKilled", "OnPlayerLeft", "OnPlayerAuthenticated", "OnLevelStarted", "OnPlayerSpawned", "OnRoundOver", "OnRoundOverTeamScores", "OnRunNextLevel", "OnLoadingLevel", "OnPunkbusterPlayerInfo");

            StopKPM();
            CleanUp();

            // resets some values so there can't be any mistakes
            this.m_strServerName = "";
            this.m_iCleanupCurrentRounds = 0;
            this.m_IsPluginEnabled = true;
            this.m_strCurrentMap = "";
            this.m_fAverageKills = 0F;
            this.m_fAverageDeaths = 0F;
            this.m_fAverageKDR = 0F;
            this.m_fAverageHSPercent = 0F;
            this.m_fAverageKPM = 0F;
            this.m_iHighestKills = 0;
            this.m_iHighestDeaths = 0;
            this.m_fHighestKDR = 0F;
            this.m_fHighestHSPercent = 0F;
            this.m_fHighestKPM = 0F;
            this.m_iTotalKills = 0;
            this.m_blSendingPlayers = false;
            this.m_iNumberOfDetectedPlayers = 0;
            this.ConsoleWrite("^2Ready to rumble!");
        }

        public void OnPluginDisable()
        {
            StopKPM();
            CleanUp();

            this.m_IsPluginEnabled = false;
            this.m_blKPMRunning = false;
            this.m_blSendingPlayers = false;
            this.ConsoleWrite("^1Taking a nap...");
        }

        // setting up the plugin's variables
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1. General settings|I have read the description", typeof(enumBoolYesNo), this.m_DescriptionRead));
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("1. General settings|Clean data up automatically? (recommended!)", typeof(enumBoolYesNo), this.m_AutomaticCleanup));
                if (this.m_AutomaticCleanup == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("1. General settings|Cleanup speed", "enum.Actions(Normal|Faster|Slower)", this.m_CleanupSpeed));
                }
                lstReturn.Add(new CPluginVariable("1. General settings|Stream to database? (recommended!)", typeof(enumBoolYesNo), this.m_StreamToDatabase));
                lstReturn.Add(new CPluginVariable("1. General settings|Scan PBBans' Master Ban Index?", typeof(enumBoolYesNo), this.m_RetrieveMBI));

                lstReturn.Add(new CPluginVariable("2. Notifications and logging|Show messages in the plugin-console?", typeof(enumBoolYesNo), this.m_SpamConsole));
                lstReturn.Add(new CPluginVariable("2. Notifications and logging|Show debugmessages in the plugin-console?", typeof(enumBoolYesNo), this.m_DebugMode));
                lstReturn.Add(new CPluginVariable("2. Notifications and logging|Show servervalues in the plugin-console?", typeof(enumBoolYesNo), this.m_ServerValues));
                lstReturn.Add(new CPluginVariable("2. Notifications and logging|Show SysTray Alerts?", typeof(enumBoolYesNo), this.m_ShowSysTrayAlert));
                lstReturn.Add(new CPluginVariable("2. Notifications and logging|Show ingame messages?", typeof(enumBoolYesNo), this.m_IngameNotifications));
                lstReturn.Add(new CPluginVariable("2. Notifications and logging|Log Data to file?", typeof(enumBoolYesNo), this.m_LogData));
                if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_LogData == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("2. Notifications and logging|Log interval (minutes)", typeof(int), this.m_iLogTime));
                }

                lstReturn.Add(new CPluginVariable("3. Cheat-detection Variables|Alter plugin variables? (use with caution!)", typeof(enumBoolYesNo), this.m_AlterVariables));
                if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_AlterVariables == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("3. Cheat-detection Variables|Ingame max. KDR", typeof(double), this.m_dIngameMaxKDR));
                    lstReturn.Add(new CPluginVariable("3. Cheat-detection Variables|Ingame max. number of kills", typeof(int), this.m_iIngameMaxKills));
                    lstReturn.Add(new CPluginVariable("3. Cheat-detection Variables|Ingame max. percentage of headshots", typeof(double), this.m_dIngameMaxHSPercent));
                    lstReturn.Add(new CPluginVariable("3. Cheat-detection Variables|Ingame max. kills per minute", typeof(double), this.m_dIngameMaxKPM));
                    lstReturn.Add(new CPluginVariable("3. Cheat-detection Variables|Global max. KDR", typeof(double), this.m_dGlobalMaxKDR));
                }

                lstReturn.Add(new CPluginVariable("4. Remove Player|Enable removing feature?", typeof(enumBoolYesNo), this.m_RemovePlayer));
                if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("4. Remove Player|Removing Option", "enum.RemoveOption(Kick|Temporary Ban|Permanent Ban)", this.m_strRemoveOption));
                    if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes && (this.m_strRemoveOption == "Permanent Ban" || this.m_strRemoveOption == "Temporary Ban"))
                    {
                        lstReturn.Add(new CPluginVariable("4. Remove Player|Ban Type", "enum.BanType(Name|GUID)", this.m_strBanType));
                    }
                    if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes && this.m_strRemoveOption == "Kick")
                    {
                        lstReturn.Add(new CPluginVariable("4. Remove Player|Kick Reason", typeof(string), this.m_strKickReason));
                    }
                    else if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes && this.m_strRemoveOption == "Temporary Ban")
                    {
                        lstReturn.Add(new CPluginVariable("4. Remove Player|Ban Time (minutes)", typeof(int), this.m_iBanTime));
                        lstReturn.Add(new CPluginVariable("4. Remove Player|Ban Reason", typeof(string), this.m_strBanReason));
                    }
                    else if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes && this.m_strRemoveOption == "Permanent Ban")
                    {
                        if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes && this.m_strBanType == "GUID")
                        {
                            lstReturn.Add(new CPluginVariable("4. Remove Player|Use PunkBuster GUID instead of EA GUID?", typeof(enumBoolYesNo), this.m_PBGUIDBan));
                        }
                        lstReturn.Add(new CPluginVariable("4. Remove Player|Perm Ban Reason", typeof(string), this.m_strPermBanReason));
                    }
                }

                lstReturn.Add(new CPluginVariable("5. Blacklisting|Enable Blacklisting feature?", typeof(enumBoolYesNo), this.m_BlackListing));
                if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_BlackListing == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("5. Blacklisting|Clan Black List", typeof(string[]), this.m_lstClanBlackList.ToArray()));
                }
            }

            return lstReturn;
        }

        // setting up the plugin's variables
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("I have read the description", typeof(enumBoolYesNo), this.m_DescriptionRead));
            lstReturn.Add(new CPluginVariable("Clean data up automatically? (recommended!)", typeof(enumBoolYesNo), this.m_AutomaticCleanup));
            lstReturn.Add(new CPluginVariable("Cleanup speed", "enum.Actions(Normal|Faster|Slower)", this.m_CleanupSpeed));
            lstReturn.Add(new CPluginVariable("Stream to database? (recommended!)", typeof(enumBoolYesNo), this.m_StreamToDatabase));
            lstReturn.Add(new CPluginVariable("Scan PBBans' Master Ban Index?", typeof(enumBoolYesNo), this.m_RetrieveMBI));
            lstReturn.Add(new CPluginVariable("Show messages in the plugin-console?", typeof(enumBoolYesNo), this.m_SpamConsole));
            lstReturn.Add(new CPluginVariable("Show debugmessages in the plugin-console?", typeof(enumBoolYesNo), this.m_DebugMode));
            lstReturn.Add(new CPluginVariable("Show servervalues in the plugin-console?", typeof(enumBoolYesNo), this.m_ServerValues));
            lstReturn.Add(new CPluginVariable("Show SysTray Alerts?", typeof(enumBoolYesNo), this.m_ShowSysTrayAlert));
            lstReturn.Add(new CPluginVariable("Show ingame messages?", typeof(enumBoolYesNo), this.m_IngameNotifications));
            lstReturn.Add(new CPluginVariable("Log Data to file?", typeof(enumBoolYesNo), this.m_LogData));
            lstReturn.Add(new CPluginVariable("Log interval (minutes)", typeof(int), this.m_iLogTime));
            lstReturn.Add(new CPluginVariable("Alter plugin variables? (use with caution!)", typeof(enumBoolYesNo), this.m_AlterVariables));
            lstReturn.Add(new CPluginVariable("Ingame max. KDR", typeof(double), this.m_dIngameMaxKDR));
            lstReturn.Add(new CPluginVariable("Ingame max. number of kills", typeof(int), this.m_iIngameMaxKills));
            lstReturn.Add(new CPluginVariable("Ingame max. percentage of headshots", typeof(double), this.m_dIngameMaxHSPercent));
            lstReturn.Add(new CPluginVariable("Ingame max. kills per minute", typeof(double), this.m_dIngameMaxKPM));
            lstReturn.Add(new CPluginVariable("Global max. KDR", typeof(double), this.m_dGlobalMaxKDR));
            lstReturn.Add(new CPluginVariable("Enable removing feature?", typeof(enumBoolYesNo), this.m_RemovePlayer));
            lstReturn.Add(new CPluginVariable("Removing Option", "enum.RemoveOption(Kick|Temporary Ban|Permanent Ban)", this.m_strRemoveOption));
            lstReturn.Add(new CPluginVariable("Ban Type", "enum.BanType(Name|GUID)", this.m_strBanType));
            lstReturn.Add(new CPluginVariable("Use PunkBuster GUID instead of EA GUID?", typeof(enumBoolYesNo), this.m_PBGUIDBan));
            lstReturn.Add(new CPluginVariable("Ban Time (minutes)", typeof(int), this.m_iBanTime));
            lstReturn.Add(new CPluginVariable("Kick Reason", typeof(string), this.m_strKickReason));
            lstReturn.Add(new CPluginVariable("Ban Reason", typeof(string), this.m_strBanReason));
            lstReturn.Add(new CPluginVariable("Perm Ban Reason", typeof(string), this.m_strPermBanReason));
            lstReturn.Add(new CPluginVariable("Enable Blacklisting feature?", typeof(enumBoolYesNo), this.m_BlackListing));
            lstReturn.Add(new CPluginVariable("Clan Black List", typeof(string[]), this.m_lstClanBlackList.ToArray()));

            return lstReturn;
        }

        // setting up the plugin's variables
        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeMinutes = 0;
            double dIngameMaxKDR = 0;
            int iIngameMaxKills = 0;
            double dIngameMaxHeadshots = 0;
            double dIngameMaxKPM = 0;
            double dGlobalMaxKDR = 0;
            int iLogTime = 0;

            if (strVariable.CompareTo("I have read the description") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_DescriptionRead = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Clean data up automatically? (recommended!)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_AutomaticCleanup = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_AutomaticCleanup == enumBoolYesNo.Yes)
                {
                    this.ConsoleWrite("I will clean up the mess periodically!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_AutomaticCleanup == enumBoolYesNo.No)
                {
                    this.ConsoleWrite("No cleaning for me! I'd rather die in dust!");
                }
            }
            else if (strVariable.CompareTo("Cleanup speed") == 0)
            {
                this.m_CleanupSpeed = strValue;
                if (this.m_CleanupSpeed.CompareTo("Normal") == 0)
                {
                    this.m_iCleanupAfterRounds = 6;
                }
                else if (this.m_CleanupSpeed.CompareTo("Faster") == 0)
                {
                    this.m_iCleanupAfterRounds = 3;
                }
                else if (this.m_CleanupSpeed.CompareTo("Slower") == 0)
                {
                    this.m_iCleanupAfterRounds = 9;
                }
            }
            else if (strVariable.CompareTo("Stream to database? (recommended!)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_StreamToDatabase = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Scan PBBans' Master Ban Index?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_RetrieveMBI = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_RetrieveMBI == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertMBIDownloader");
                    this.ExecuteCommand("procon.protected.tasks.add", "CheaterAlertMBIDownloader", "0", "21600", "-1", "procon.protected.plugins.call", "CheaterAlert", "DownloadMBI");
                }
                else if (this.m_RetrieveMBI == enumBoolYesNo.No)
                {
                    this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertMBIDownloader");
                }
            }
            else if (strVariable.CompareTo("Show messages in the plugin-console?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_SpamConsole = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_SpamConsole == enumBoolYesNo.Yes)
                {
                    this.ConsoleWrite("I will comment every action taken!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.No)
                {
                    this.ConsoleWrite("I will shut my mouth and not bother you!");
                }
            }
            else if (strVariable.CompareTo("Show debugmessages in the plugin-console?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_DebugMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Show servervalues in the plugin-console?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ServerValues = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_ServerValues == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertServerValues");
                    this.ExecuteCommand("procon.protected.tasks.add", "CheaterAlertServerValues", "0", "60", "-1", "procon.protected.plugins.call", "CheaterAlert", "ServerValuesDisplay");
                }
                else if (this.m_ServerValues == enumBoolYesNo.No)
                {
                    this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertServerValues");
                }
            }
            else if (strVariable.CompareTo("Show SysTray Alerts?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ShowSysTrayAlert = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Show ingame messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_IngameNotifications = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_IngameNotifications == enumBoolYesNo.Yes)
                {
                    this.ConsoleWrite("Ingame-Players will be notified!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_IngameNotifications == enumBoolYesNo.No)
                {
                    this.ConsoleWrite("Ingame-Players won't be notified!");
                }
            }
            else if (strVariable.CompareTo("Log Data to file?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_LogData = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_LogData == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertLogger");
                    this.ExecuteCommand("procon.protected.tasks.add", "CheaterAlertLogger", "0", (this.m_iLogTime * 60).ToString(), "-1", "procon.protected.plugins.call", "CheaterAlert", "Logging");
                }
                else if (this.m_LogData == enumBoolYesNo.No)
                {
                    this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertLogger");
                }
            }
            else if (strVariable.CompareTo("Log interval (minutes)") == 0 && int.TryParse(strValue, out iLogTime) == true)
            {
                if (iLogTime > 0)
                {
                    this.m_iLogTime = iLogTime;
                    if (this.m_LogData == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertLogger");
                        this.ExecuteCommand("procon.protected.tasks.add", "CheaterAlertLogger", "0", (this.m_iLogTime * 60).ToString(), "-1", "procon.protected.plugins.call", "CheaterAlert", "Logging");
                    }
                }
            }
            else if (strVariable.CompareTo("Alter plugin variables? (use with caution!)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_AlterVariables = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_AlterVariables == enumBoolYesNo.No)
                {
                    this.m_iIngameMaxKills = 50;
                    this.m_dIngameMaxKDR = 6.0;
                    this.m_dIngameMaxHSPercent = 75;
                    this.m_dIngameMaxKPM = 2.0;
                    this.m_dGlobalMaxKDR = 3.5;
                }
            }
            else if (strVariable.CompareTo("Ingame max. KDR") == 0 && double.TryParse(strValue, out dIngameMaxKDR) == true)
            {
                if (dIngameMaxKDR > 0)
                {
                    this.m_dIngameMaxKDR = dIngameMaxKDR;
                }
            }
            else if (strVariable.CompareTo("Ingame max. number of kills") == 0 && int.TryParse(strValue, out iIngameMaxKills) == true)
            {
                if (iIngameMaxKills > 0)
                {
                    this.m_iIngameMaxKills = iIngameMaxKills;
                }
            }
            else if (strVariable.CompareTo("Ingame max. percentage of headshots") == 0 && double.TryParse(strValue, out dIngameMaxHeadshots) == true)
            {
                if (dIngameMaxHeadshots <= 100 && dIngameMaxHeadshots >= 1)
                {
                    this.m_dIngameMaxHSPercent = dIngameMaxHeadshots;
                }
                else
                {
                    this.ConsoleWrite("Percentage of headshots must be a number between 0 and 100!");
                }
            }
            else if (strVariable.CompareTo("Ingame max. kills per minute") == 0 && double.TryParse(strValue, out dIngameMaxKPM) == true)
            {
                if (dIngameMaxKPM > 0)
                {
                    this.m_dIngameMaxKPM = dIngameMaxKPM;
                }
            }
            else if (strVariable.CompareTo("Global max. KDR") == 0 && double.TryParse(strValue, out dGlobalMaxKDR) == true)
            {
                if (dGlobalMaxKDR > 0)
                {
                    this.m_dGlobalMaxKDR = dGlobalMaxKDR;
                }
            }
            else if (strVariable.CompareTo("Enable removing feature?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_RemovePlayer = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.Yes)
                {
                    this.ConsoleWrite("I will remove suspicious players from this server!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_RemovePlayer == enumBoolYesNo.No)
                {
                    this.ConsoleWrite("I won't remove any suspicious players!");
                }
            }
            else if (strVariable.CompareTo("Removing Option") == 0)
            {
                this.m_strRemoveOption = strValue;
                if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_strRemoveOption == "Kick")
                {
                    this.ConsoleWrite("Naughty players will just get kicked!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_strRemoveOption == "Temporary Ban")
                {
                    this.ConsoleWrite("Naughty players will be banned!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_strRemoveOption == "Permanent Ban")
                {
                    this.ConsoleWrite("I will get rid of naugthy people permanently!");
                }
            }
            else if (strVariable.CompareTo("Ban Type") == 0)
            {
                this.m_strBanType = strValue;
                if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_strBanType == "Name")
                {
                    this.ConsoleWrite("Their name will help me to get rid of players!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_strBanType == "GUID")
                {
                    this.ConsoleWrite("I will use the GUID to get rid of players!");
                }
            }
            else if (strVariable.CompareTo("Use PunkBuster GUID instead of EA GUID?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_PBGUIDBan = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ban Time (minutes)") == 0 && int.TryParse(strValue, out iTimeMinutes) == true)
            {
                if (iTimeMinutes > 0)
                {
                    this.m_iBanTime = iTimeMinutes;
                    if (this.m_SpamConsole == enumBoolYesNo.Yes)
                    {
                        this.ConsoleWrite(String.Format("The bad boys will be banned for {0} minutes!", this.m_iBanTime));
                    }
                }
            }
            else if (strVariable.CompareTo("Kick Reason") == 0)
            {
                this.m_strKickReason = strValue;
            }
            else if (strVariable.CompareTo("Ban Reason") == 0)
            {
                this.m_strBanReason = strValue;
            }
            else if (strVariable.CompareTo("Perm Ban Reason") == 0)
            {
                this.m_strPermBanReason = strValue;
            }
            else if (strVariable.CompareTo("Enable Blacklisting feature?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_BlackListing = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_BlackListing == enumBoolYesNo.Yes)
                {
                    this.ConsoleWrite("Some people will get thrown out immediatelly!");
                }
                else if (this.m_SpamConsole == enumBoolYesNo.Yes && this.m_BlackListing == enumBoolYesNo.No)
                {
                    this.ConsoleWrite("All people are equal, noone will get kicked out immediatelly!");
                }
            }
            else if (strVariable.CompareTo("Clan Black List") == 0)
            {
                this.m_lstClanBlackList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
        }


        #endregion

        #region Used Interfaces

        public override void OnPlayerJoin(string strSoldierName)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // check whether the playerdata-dictionary already contains an entry for that soldier, adding one if not
                if (this.m_dicPlayerData.ContainsKey(strSoldierName) == false)
                {
                    this.m_dicPlayerData.Add(strSoldierName, new PlayerStats(strSoldierName, ""));
                }
                if (this.m_RetrieveMBI == enumBoolYesNo.Yes)
                {
                    new Thread(SearchMBI).Start(strSoldierName);
                }
            }
        }

        public override void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                if (this.m_dicPlayerGUID.ContainsKey(strSoldierName) == false)
                {
                    this.m_dicPlayerGUID.Add(strSoldierName, strGuid);
                }
                if (this.m_RetrieveMBI == enumBoolYesNo.Yes)
                {
                    new Thread(SearchMBI).Start(strSoldierName);
                }
            }
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // saves the Punkbuster-data for a player
                if (this.m_dicPlayerPunkbusterInfo.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.m_dicPlayerPunkbusterInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                }
                if (this.m_dicPlayerPBGUID.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.m_dicPlayerPBGUID.Add(cpbiPlayer.SoldierName, cpbiPlayer.GUID);
                }
            }
        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                bool detected;
                if (this.m_dicDetectedPlayers.ContainsValue(cpiPlayer.SoldierName) == true)
                {
                    SendDetectedPlayers();
                    if (this.m_LogData == enumBoolYesNo.Yes)
                    {
                        Logging();
                    }
                    detected = true;
                }
                else
                {
                    detected = false;
                }

                // removes some of the player's data when he leaves and isn't listed as a detected player
                if (this.m_dicPlayerData.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.m_iTotalKills -= this.m_dicPlayerData[cpiPlayer.SoldierName].Kills;
                    this.m_dicPlayerData.Remove(cpiPlayer.SoldierName);
                }
                if (this.m_dicPlayerFrostbiteInfo.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.m_dicPlayerFrostbiteInfo.Remove(cpiPlayer.SoldierName);
                }
                if (this.m_dicPlayerPunkbusterInfo.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.m_dicPlayerPunkbusterInfo.Remove(cpiPlayer.SoldierName);
                }
                if (this.m_dicPlayerGUID.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.m_dicPlayerGUID.Remove(cpiPlayer.SoldierName);
                }
                if (this.m_dicPlayerPBGUID.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.m_dicPlayerPBGUID.Remove(cpiPlayer.SoldierName);
                }
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes && this.m_iPlayerCount >= 4)
            {
                // makes sure the PlayerList used by other methodes is up-to-date
                this.m_lstPlayerList = lstPlayers;

                // checks whether the kills per minute-calculation is already running, starting it if not
                if (this.m_blKPMRunning == false)
                {
                    StartKPM();
                }

                // calculate some average values
                CalculateAverages();

                this.m_iHighestKills = 0;
                this.m_iHighestDeaths = 0;
                this.m_fHighestKDR = 0F;
                this.m_fHighestHSPercent = 0F;
                this.m_fHighestKPM = 0F;

                foreach (CPlayerInfo player in lstPlayers)
                {
                    if (this.m_BlackListing == enumBoolYesNo.Yes && ClanTagValidation(player.ClanTag) == true)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", player.SoldierName, "[Cheater Alert] Your clan isn't welcome on this server!");
                    }
                    else
                    {

                        // some checks whether all dictionaries contain the needed entries
                        if (this.m_dicPlayerData.ContainsKey(player.SoldierName) == false)
                        {
                            this.m_dicPlayerData.Add(player.SoldierName, new PlayerStats(player.SoldierName, player.ClanTag));
                        }
                        else if (this.m_dicPlayerData.ContainsKey(player.SoldierName) == true && this.m_dicPlayerData[player.SoldierName].ClanTag.CompareTo("") == 0)
                        {
                            this.m_dicPlayerData[player.SoldierName].ClanTag = player.ClanTag;
                        }
                        if (this.m_dicPlayerGUID.ContainsKey(player.SoldierName) == false)
                        {
                            this.m_dicPlayerGUID.Add(player.SoldierName, player.GUID);
                        }
                        if (this.m_dicPlayerFrostbiteInfo.ContainsKey(player.SoldierName) == false)
                        {
                            this.m_dicPlayerFrostbiteInfo.Add(player.SoldierName, player);
                        }

                        // using the Cheatdetection-algorithms for each player
                        if (this.m_iTotalKills > 32)
                        {
                            if (this.m_dicPlayerData.ContainsKey(player.SoldierName) == true && this.m_dicPlayerData[player.SoldierName].Kills > 10)
                            {
                                CheckIngameKDR(player.SoldierName);
                                if (this.m_dicPlayerData[player.SoldierName].SuspectednessKDR > 3)
                                {
                                    CheckIngameKills(player.SoldierName);
                                    if (this.m_dicPlayerData[player.SoldierName].SuspectednessKills > 3)
                                    {
                                        CheckIngameHSPercent(player.SoldierName);
                                        if (this.m_dicPlayerData[player.SoldierName].SuspectednessHSPercent > 3)
                                        {
                                            CheckIngameKPM(player.SoldierName);
                                            if (this.m_dicPlayerData[player.SoldierName].SuspectednessKPM > 3)
                                            {
                                                CheckGlobalKDR(player.SoldierName);
                                            }
                                        }
                                    }
                                }
                                this.m_dicPlayerData[player.SoldierName].CheckStatus();
                            }
                        }

                        // setting the values for highestKills etc.
                        if (player.Kills > this.m_iHighestKills)
                        {
                            this.m_iHighestKills = player.Kills;
                        }
                        if (player.Deaths > this.m_iHighestDeaths)
                        {
                            this.m_iHighestDeaths = player.Deaths;
                        }
                        if (player.Kdr > this.m_fHighestKDR)
                        {
                            this.m_fHighestKDR = player.Kdr;
                        }
                        float HSPercent = this.m_dicPlayerData[player.SoldierName].CalculateHSPercent();
                        if (HSPercent > this.m_fHighestHSPercent)
                        {
                            this.m_fHighestHSPercent = HSPercent;
                        }
                        float KPM = this.m_dicPlayerData[player.SoldierName].CalculateKPM();
                        if (KPM > this.m_fHighestKPM)
                        {
                            this.m_fHighestKPM = KPM;
                        }
                    }
                }
            }
        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                CPlayerInfo Killer = kKillerVictimDetails.Killer;
                CPlayerInfo Victim = kKillerVictimDetails.Victim;
                // getting the killer's weapon-name
                Weapon tmpWeapon = this.GetWeaponDefines()[kKillerVictimDetails.DamageType];
                string weaponUsed = this.GetLocalized(tmpWeapon.Name, String.Format("global.Weapons.{0}", kKillerVictimDetails.DamageType.ToLower()));

                // just counting stats if the kill is no suicide
                if (kKillerVictimDetails.IsSuicide == false)
                {
                    // another check whether the playerdata-dictionary contains an entry for the player
                    if (this.m_dicPlayerData.ContainsKey(Killer.SoldierName) == false)
                    {
                        this.m_dicPlayerData.Add(Killer.SoldierName, new PlayerStats(Killer.SoldierName, Killer.ClanTag));
                    }
                    else if (this.m_dicPlayerData.ContainsKey(Killer.SoldierName) == true)
                    {
                        // counting up kills/headshots for the killer
                        this.m_dicPlayerData[Killer.SoldierName].Kills++;
                        this.m_dicPlayerData[Killer.SoldierName].Weapon = weaponUsed;
                        if (kKillerVictimDetails.Headshot == true)
                        {
                            this.m_dicPlayerData[Killer.SoldierName].Headshots++;
                        }
                    }

                    if (this.m_dicPlayerData.ContainsKey(Victim.SoldierName) == false)
                    {
                        this.m_dicPlayerData.Add(Victim.SoldierName, new PlayerStats(Victim.SoldierName, Victim.ClanTag));
                    }
                    else if (this.m_dicPlayerData.ContainsKey(Victim.SoldierName) == true)
                    {
                        // counting up the victim's deaths
                        this.m_dicPlayerData[Victim.SoldierName].Deaths++;
                    }

                    // counting up the total number of kills
                    this.m_iTotalKills++;
                }
            }
        }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // planned: storing data about weapons/gadgets
            }
        }

        public override void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // planned: monitoring chat on high-status
            }
        }

        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // planned: monitoring chat on high-status
            }
        }

        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // planned: monitoring chat on high-status
            }
        }

        public override void OnLevelStarted()
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // resetting some values
                this.m_fAverageKills = 0F;
                this.m_fAverageDeaths = 0F;
                this.m_fAverageKDR = 0F;
                this.m_fAverageHSPercent = 0F;
                this.m_fAverageKPM = 0F;
                this.m_iHighestKills = 0;
                this.m_iHighestDeaths = 0;
                this.m_fHighestKDR = 0F;
                this.m_fHighestHSPercent = 0F;
                this.m_fHighestKPM = 0F;
                this.m_iTotalKills = 0;
                this.m_iNumberOfDetectedPlayers = 0;

                if (this.m_iPlayerCount > 0)
                {
                    // checking the KPM-calculation
                    if (this.m_blKPMRunning == false)
                    {
                        StartKPM();
                    }

                    // cleaning up data to prevent lag
                    if (this.m_AutomaticCleanup == enumBoolYesNo.Yes)
                    {
                        if (this.m_iCleanupCurrentRounds >= this.m_iCleanupAfterRounds)
                        {
                            new Thread(CleanUp);
                        }
                        else if (this.m_iCleanupCurrentRounds < this.m_iCleanupAfterRounds)
                        {
                            this.m_iCleanupCurrentRounds++;
                        }
                    }
                }
            }
        }

        public override void OnRunNextLevel()
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                if (this.m_StreamToDatabase == enumBoolYesNo.Yes && this.m_iPlayerCount > 0)
                {
                    // sending serverstats
                    SendServerStats();
                }

                StopKPM();
            }
        }

        public override void OnLoadingLevel(string strMapFileName, int roundsPlayed, int roundsTotal)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // correcting mapname and shortly halting the KPM-calculation
                this.m_strCurrentMap = strMapFileName;
                StopKPM();
            }
        }

        public override void OnRoundOver(int iWinningTeamID)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                StopKPM();

                this.m_iHighestKills = 0;
                this.m_iHighestDeaths = 0;
                this.m_fHighestKDR = 0F;
                this.m_fHighestHSPercent = 0F;
                this.m_fHighestKPM = 0F;

                foreach (CPlayerInfo player in this.m_lstPlayerList)
                {
                    // last check whether highest stats are correct
                    if (player.Kills > this.m_iHighestKills)
                    {
                        this.m_iHighestKills = player.Kills;
                    }
                    if (player.Deaths > this.m_iHighestDeaths)
                    {
                        this.m_iHighestDeaths = player.Deaths;
                    }
                    if (player.Kdr > this.m_fHighestKDR)
                    {
                        this.m_fHighestKDR = player.Kdr;
                    }
                    float HSPercent = this.m_dicPlayerData[player.SoldierName].CalculateHSPercent();
                    if (HSPercent > this.m_fHighestHSPercent)
                    {
                        this.m_fHighestHSPercent = HSPercent;
                    }
                    float KPM = this.m_dicPlayerData[player.SoldierName].CalculateKPM();
                    if (KPM > this.m_fHighestKPM)
                    {
                        this.m_fHighestKPM = KPM;
                    }
                }

                // sending stats to the mySQL-database
                if (this.m_StreamToDatabase == enumBoolYesNo.Yes && this.m_iPlayerCount > 0)
                {
                    SendServerStats();
                    SendDetectedPlayers();
                }
                foreach (KeyValuePair<string, CheaterAlert.PlayerStats> kvp in this.m_dicPlayerData)
                {
                    // resetting some playerstats
                    kvp.Value.Reset();
                }
            }
        }

        public override void OnRoundOverTeamScores(List<TeamScore> lstTeamScores)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // planned: maybe include some score-checking at the end of the round
            }
        }

        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            if (this.m_DescriptionRead == enumBoolYesNo.Yes)
            {
                // keeping the server-info up-to-date
                this.m_strServerName = csiServerInfo.ServerName;
                this.m_strCurrentMap = ChangeMapName(csiServerInfo.Map);
                this.m_iPlayerCount = csiServerInfo.PlayerCount;
                this.m_csiServerInfo = csiServerInfo;
            }
        }

        #endregion

        #region Ingame-Commands

        // planned: including some ingame-commands to force a check, show some data, ...

        #endregion

        #region Methods

        #region General methods

        // clean up stored data. Used to prevent lags when running the plugin for a long time
        public void CleanUp()
        {
            this.m_dicPlayerData.Clear();
            this.m_dicPlayerGUID.Clear();
            this.m_dicPlayerPBGUID.Clear();
            this.m_dicPlayerFrostbiteInfo.Clear();
            this.m_dicPlayerPunkbusterInfo.Clear();
            PlayerDetected();
            while (this.m_blSendingPlayers == true)
            {
                Thread.Sleep(5000);
            }
            this.m_dicDetectedPlayers.Clear();
            this.m_dicKillsStart.Clear();
            this.m_lstServerValues.Clear();
            this.m_lstPlayerList.Clear();
            System.GC.Collect();
            if (this.m_SpamConsole == enumBoolYesNo.Yes)
            {
                this.ConsoleWrite("Seems like I lost all my memory. Let's start from the scratch!");
            }
            this.m_iCleanupCurrentRounds = 0;
        }

        #region Map functions

        // replaces the filename with a better-to-read map name
        public string ChangeMapName(string strOldName)
        {
            if (strOldName.CompareTo("Levels/MP_001") == 0)
            {
                this.m_strCurrentMap = "Conquest Panama Canal";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_001SR") == 0)
            {
                this.m_strCurrentMap = "SR Panama Canal";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_001SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Panama Canal";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_002") == 0)
            {
                this.m_strCurrentMap = "Rush Valparaiso";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_002SR") == 0)
            {
                this.m_strCurrentMap = "SR Valparaiso";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_003") == 0)
            {
                this.m_strCurrentMap = "Conquest Laguna Alta";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_003SR") == 0)
            {
                this.m_strCurrentMap = "SR Laguna Alta";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_004") == 0)
            {
                this.m_strCurrentMap = "Rush Isla Inocentes";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_004SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Isla Inocentes";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_005") == 0)
            {
                this.m_strCurrentMap = "Conquest Atacama Desert";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_005GR") == 0)
            {
                this.m_strCurrentMap = "Rush Atacama Desert";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_005SR") == 0)
            {
                this.m_strCurrentMap = "SR Atacama Desert";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_006CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Arica Harbor";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_006") == 0)
            {
                this.m_strCurrentMap = "Rush Arica Harbor";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_006SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Arica Harbor";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_007") == 0)
            {
                this.m_strCurrentMap = "Conquest White Pass";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_007GR") == 0)
            {
                this.m_strCurrentMap = "Rush White Pass";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_007SDM") == 0)
            {
                this.m_strCurrentMap = "SDM White Pass";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_008") == 0)
            {
                this.m_strCurrentMap = "Rush Nelson Bay";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_008CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Nelson Bay";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_008SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Nelson Bay";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_009GR") == 0)
            {
                this.m_strCurrentMap = "Rush Laguna Presa";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_009CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Laguna Presa";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_009SR") == 0)
            {
                this.m_strCurrentMap = "SR Laguna Presa";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_012GR") == 0)
            {
                this.m_strCurrentMap = "Rush Port Valdez";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_012CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Port Valdez";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_012SR") == 0)
            {
                this.m_strCurrentMap = "SR Port Valdez";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Harvest_Day_GR") == 0)
            {
                this.m_strCurrentMap = "Rush Harvest Day";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Harvest_Day_CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Harvest Day";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Harvest_Day_SR") == 0)
            {
                this.m_strCurrentMap = "SR Harvest Day";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Harvest_Day_SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Harvest Day";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Oasis_GR") == 0)
            {
                this.m_strCurrentMap = "Rush Oasis";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Oasis_CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Oasis";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Oasis_SR") == 0)
            {
                this.m_strCurrentMap = "SR Oasis";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/BC1_Oasis_SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Oasis";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_SP_002GR") == 0)
            {
                this.m_strCurrentMap = "Rush Cold War";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_SP_002SR") == 0)
            {
                this.m_strCurrentMap = "SR Cold War";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_SP_002SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Cold War";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_SP_005CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Heavy Metal";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/MP_SP_005SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Heavy Metal";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_002CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Vantage Point";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_002R") == 0)
            {
                this.m_strCurrentMap = "Rush Vantage Point";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_002SR") == 0)
            {
                this.m_strCurrentMap = "SR Vantage Point";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_002SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Vantage Point";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_003CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Hill 137";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_003R") == 0)
            {
                this.m_strCurrentMap = "Rush Hill 137";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_003SR") == 0)
            {
                this.m_strCurrentMap = "SR Hill 137";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_003SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Hill 137";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_005CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Cao Son Temple";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_005R") == 0)
            {
                this.m_strCurrentMap = "Rush Cao Son Temple";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_005SR") == 0)
            {
                this.m_strCurrentMap = "SR Cao Son Temple";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_005SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Cao Son Temple";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_006CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Phu Bai Valley";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_006R") == 0)
            {
                this.m_strCurrentMap = "Rush Phu Bai Valley";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_006SR") == 0)
            {
                this.m_strCurrentMap = "SR Phu Bai Valley";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_006SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Phu Bai Valley";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_007CQ") == 0)
            {
                this.m_strCurrentMap = "Conquest Operation Hastings";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_007R") == 0)
            {
                this.m_strCurrentMap = "Rush Operation Hastings";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_007SR") == 0)
            {
                this.m_strCurrentMap = "SR Operation Hastings";
                return this.m_strCurrentMap;
            }
            else if (strOldName.CompareTo("Levels/NAM_MP_007SDM") == 0)
            {
                this.m_strCurrentMap = "SDM Operation Hastings";
                return this.m_strCurrentMap;
            }
            else
            {
                this.m_strCurrentMap = strOldName;
                return this.m_strCurrentMap;
            }
        }

        #endregion

        // executing a Punkbuster-Command
        public void PBCommand(string command)
        {
            this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", command);
        }

        public bool ClanTagValidation(string ClanTag)
        {
            foreach (string BlackTag in this.m_lstClanBlackList)
            {
                if (BlackTag.ToLower().CompareTo(ClanTag.ToLower()) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        // method to encode a string
        public string EncodeMD5(string text)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.ASCII.GetBytes(text);
            data = x.ComputeHash(data);
            string encoded = "";
            for (int i = 0; i < data.Length; i++)
            {
                encoded += data[i].ToString("x2").ToLower();
            }
            return encoded;
        }

        #endregion

        #region Notifications & Logging

        // method to write a CheaterAlert-message to the plugin console
        public void ConsoleWrite(string message)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^8Cheater Alert^0: {0}", message));
        }

        // method to write a CheaterAlert-debugmessage to the plugin console
        public void ConsoleDebug(string message)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("CheaterAlert Debug: {0}", message));
        }

        // method to create a popup at the Systray
        public void SysTrayAlert(string strMessage, string blImportant)
        {
            this.ExecuteCommand("procon.protected.notification.write", "Cheater Alert", strMessage, blImportant);
        }

        // sends an ingame-message to all players
        public void IngameSayAll(string message)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", "Cheater Alert: " + message, "all");
        }

        // creates ServerValuesMessages
        public void ServerValuesMessages()
        {
            this.m_lstServerValues = new List<string>();
            this.m_lstServerValues.Clear();

            this.m_lstServerValues.Add(DateTime.Now.ToString());
            this.m_lstServerValues.Add("=====Cheater Alert - Current Server Values=====");
            this.m_lstServerValues.Add("ServerName: " + this.m_strServerName);
            this.m_lstServerValues.Add("ServerAddress: " + this.m_strServerAddress);
            this.m_lstServerValues.Add("PlayerCount: " + this.m_iPlayerCount.ToString());
            this.m_lstServerValues.Add("Map: " + this.m_strCurrentMap);
            this.m_lstServerValues.Add("AverageKills: " + this.m_fAverageKills.ToString());
            this.m_lstServerValues.Add("AverageDeaths: " + this.m_fAverageDeaths.ToString());
            this.m_lstServerValues.Add("AverageKDR: " + this.m_fAverageKDR.ToString());
            this.m_lstServerValues.Add("AverageHSPercent: " + this.m_fAverageHSPercent.ToString());
            this.m_lstServerValues.Add("AverageKPM: " + this.m_fAverageKPM.ToString());
            this.m_lstServerValues.Add("HighestKills: " + this.m_iHighestKills.ToString());
            this.m_lstServerValues.Add("HighestDeaths: " + this.m_iHighestDeaths.ToString());
            this.m_lstServerValues.Add("HighestKDR: " + this.m_fHighestKDR.ToString());
            this.m_lstServerValues.Add("HighestHSPercent: " + this.m_fHighestHSPercent.ToString());
            this.m_lstServerValues.Add("HighestKPM: " + this.m_fHighestKPM.ToString());
            this.m_lstServerValues.Add("=====Cheater Alert - End of Values=====");
        }

        // writing some servervalues to the plugin console
        public void ServerValues()
        {
            if (this.m_ServerValues == enumBoolYesNo.Yes)
            {
                foreach (string ServerValue in this.m_lstServerValues)
                {
                    this.ConsoleDebug(ServerValue);
                }
                this.m_lstServerValues.Clear();
            }
        }

        // displays some server values within the plugin console
        public void ServerValuesDisplay()
        {
            if (this.m_ServerValues == enumBoolYesNo.Yes && this.m_iPlayerCount >= 4)
            {
                ServerValuesMessages();
                ServerValues();
            }
        }

        // checking & creating log files
        bool CreateLogFiles()
        {
            bool sv = true;
            bool dp = true;

            #region ServerValues

            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Checking whether directory to store servervalues exists");
            }
            DirectoryInfo di = new DirectoryInfo(@"Plugins/BFBC2/CheaterAlert/Logs/ServerValues");
            if (!di.Exists)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Servervalues-Directory doesn't exist. Trying to create it");
                }
                di.Create();
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Created Servervalues-directory successfully");
                }
            }
            else
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Servervalues-Directory already exists");
                }
            }
            string filename = DateTime.Now.ToString("dd_MMM_yyyy");
            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Checking whether most recent Servervalues-logfile exists");
            }
            this.m_strLogFilePathValues = @"Plugins/BFBC2/CheaterAlert/Logs/ServerValues/" + filename + ".txt";
            FileInfo fi = new FileInfo(this.m_strLogFilePathValues);
            if (!fi.Exists)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Servervalues-Logfile doesn't exist. Trying to create it");
                }
                fi.Create();
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Servervalues-Created logfile successfully");
                }
                sv = false;
            }
            else
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Servervalues-Logfile already exists");
                }
                sv = true;
            }

            #endregion

            #region DetectedPlayers

            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Checking whether directory to store detected players exists");
            }
            DirectoryInfo di2 = new DirectoryInfo(@"Plugins/BFBC2/CheaterAlert/Logs/DetectedPlayers");
            if (!di2.Exists)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("DetectedPlayers-Directory doesn't exist. Trying to create it");
                }
                di2.Create();
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Created DetectedPlayers-directory successfully");
                }
            }
            else
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("DetectedPlayers-Directory already exists");
                }
            }
            string filename2 = DateTime.Now.ToString("dd_MMM_yyyy");
            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Checking whether most recent DetectedPlayers-logfile exists");
            }
            this.m_strLogFilePathPlayers = @"Plugins/BFBC2/CheaterAlert/Logs/DetectedPlayers/" + filename2 + ".txt";
            FileInfo fi2 = new FileInfo(this.m_strLogFilePathPlayers);
            if (!fi2.Exists)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("DetectedPlayers-Logfile doesn't exist. Trying to create it");
                }
                fi2.Create();
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("DetectedPlayers-Created logfile successfully");
                }
                dp = false;
            }
            else
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("DetectedPlayers-Logfile already exists");
                }
                dp = true;
            }

            #endregion

            if (sv == true && dp == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // writing some values to a file
        public void LogData()
        {
            if (this.m_LogData == enumBoolYesNo.Yes)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Starting to write data to log files");
                }

                if (this.m_lstServerValues.Count > 0)
                {
                    TextWriter tw = new StreamWriter(this.m_strLogFilePathValues, true);
                    foreach (string ServerValue in this.m_lstServerValues)
                    {
                        tw.WriteLine(ServerValue);
                    }
                    tw.WriteLine(tw.NewLine);
                    tw.Close();
                }

                if (this.m_iNumberOfDetectedPlayers > 0)
                {
                    TextWriter tw2 = new StreamWriter(this.m_strLogFilePathPlayers, true);
                    for (int i = 0; i < this.m_iNumberOfDetectedPlayers; i++)
                    {
                        string Player = this.m_dicDetectedPlayers[i].ToString();
                        if (this.m_dicPlayerData[Player].AlreadySent == false)
                        {
                            tw2.WriteLine(DateTime.Now.ToString());
                            tw2.WriteLine("Name: " + Player);
                            tw2.WriteLine("ClanTag: " + this.m_dicPlayerData[Player].ClanTag);
                            if (this.m_dicPlayerGUID.ContainsKey(Player) == true)
                            {
                                tw2.WriteLine("GUID: " + this.m_dicPlayerGUID[Player]);

                            }
                            else
                            {
                                tw2.WriteLine("GUID: UNKNOWN");
                            }
                            if (this.m_dicPlayerPunkbusterInfo.ContainsKey(Player) == true)
                            {
                                tw2.WriteLine("PB GUID: " + this.m_dicPlayerPunkbusterInfo[Player].GUID);
                            }
                            else
                            {
                                tw2.WriteLine("PB GUID: UNKNOWN");
                            }
                            tw2.WriteLine("Kills: " + this.m_dicPlayerData[Player].Kills);
                            tw2.WriteLine("Deaths " + this.m_dicPlayerData[Player].Deaths);
                            tw2.WriteLine("KDR: " + this.m_dicPlayerData[Player].CalculateKDR());
                            tw2.WriteLine("HSPercent: " + this.m_dicPlayerData[Player].CalculateHSPercent());
                            tw2.WriteLine("KPM: " + this.m_dicPlayerData[Player].CalculateKPM());
                            tw2.WriteLine("Global KDR: " + this.m_dicPlayerData[Player].GlobalStats.kdr);
                            tw2.WriteLine("Status: " + this.m_dicPlayerData[Player].CheckStatus());
                            tw2.WriteLine("Suspectedness: " + this.m_dicPlayerData[Player].Suspectedness);
                            tw2.WriteLine("SuspectednessKDR: " + this.m_dicPlayerData[Player].SuspectednessKDR);
                            tw2.WriteLine("SuspectednessKPM: " + this.m_dicPlayerData[Player].SuspectednessKPM);
                            tw2.WriteLine("SuspectednessHSPercent: " + this.m_dicPlayerData[Player].SuspectednessHSPercent);
                            tw2.WriteLine("SuspectednessKills: " + this.m_dicPlayerData[Player].SuspectednessKills);
                            tw2.WriteLine("ServerName: " + this.m_strServerName);
                            tw2.WriteLine(tw2.NewLine);
                        }
                    }
                    tw2.Close();
                }

                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Finished writing data to log files");
                }
            }
        }

        // method used to log data to files, checks folder, creates messages, and writes them
        public void Logging()
        {
            if (this.m_iPlayerCount >= 4)
            {
                if (CreateLogFiles() == false)
                {
                    Thread.Sleep(2000);
                }
                ServerValuesMessages();
                LogData();
            }
        }

        public void PlayerDetected()
        {
            if (this.m_LogData == enumBoolYesNo.Yes)
            {
                CreateLogFiles();
                LogData();
            }
            SendDetectedPlayers();
        }

        #endregion

        #region Cheater Detection

        #region KPM calculation

        public void StartKPM()
        {
            this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertKPM");
            this.ExecuteCommand("procon.protected.tasks.add", "CheaterAlertKPM", "0", "60", "-1", "procon.protected.plugins.call", "CheaterAlert", "KPMCalculation");
            this.m_blKPMRunning = true;
        }

        public void StopKPM()
        {
            this.ExecuteCommand("procon.protected.tasks.remove", "CheaterAlertKPM");
            this.m_blKPMRunning = false;
        }

        public void KPMCalculation()
        {
            if (this.m_blMinuteOver == false)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Starting KPM-Calculation");
                }
                this.m_dicKillsStart.Clear();
                foreach (CPlayerInfo player in this.m_lstPlayerList)
                {
                    this.m_dicKillsStart.Add(player.SoldierName, player.Kills);
                }
                this.m_blMinuteOver = true;
            }
            else
            {
                foreach (CPlayerInfo player in this.m_lstPlayerList)
                {
                    if (this.m_dicKillsStart.ContainsKey(player.SoldierName) == true)
                    {
                        int KPM = player.Kills - this.m_dicKillsStart[player.SoldierName];
                        if (this.m_dicPlayerData.ContainsKey(player.SoldierName) == true)
                        {
                            this.m_dicPlayerData[player.SoldierName].sumKPM += KPM;
                            this.m_dicPlayerData[player.SoldierName].numKPM++;
                        }
                        else
                        {
                            this.m_dicPlayerData.Add(player.SoldierName, new CheaterAlert.PlayerStats(player.SoldierName, player.ClanTag));
                            this.m_dicPlayerData[player.SoldierName].sumKPM += KPM;
                            this.m_dicPlayerData[player.SoldierName].numKPM++;
                        }
                    }
                }
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Stopping KPM-Calculation");
                }
                this.m_blMinuteOver = false;
            }
        }

        #endregion

        #region Calculating averages

        // calculates the average KDR
        public float AverageKDR()
        {
            if (this.m_iPlayerCount > 0)
            {
                float tempKDR = 0F;
                foreach (CPlayerInfo player in this.m_lstPlayerList)
                {
                    tempKDR += player.Kdr;
                }
                this.m_fAverageKDR = (float)tempKDR / (float)this.m_iPlayerCount;
                return this.m_fAverageKDR;
            }
            else
            {
                return 0F;
            }
        }

        // calculates the average number of kills
        public float AverageKills()
        {
            if (this.m_iPlayerCount > 0)
            {
                float tempKills = 0F;
                foreach (CPlayerInfo player in this.m_lstPlayerList)
                {
                    tempKills += player.Kills;
                }
                this.m_fAverageKills = (float)tempKills / (float)this.m_iPlayerCount;
                return this.m_fAverageKills;
            }
            else
            {
                return 0F;
            }
        }

        // calculates the average number of deaths
        public float AverageDeaths()
        {
            if (this.m_iPlayerCount > 0)
            {
                float tempDeaths = 0F;
                foreach (CPlayerInfo player in this.m_lstPlayerList)
                {
                    tempDeaths += player.Deaths;
                }
                this.m_fAverageDeaths = (float)tempDeaths / (float)this.m_iPlayerCount;
                return this.m_fAverageDeaths;
            }
            else
            {
                return 0F;
            }
        }

        // calculates the average percentage of headshots
        public float AverageHSPercent()
        {
            int iStoredPlayers = this.m_dicPlayerData.Count;
            if (this.m_iPlayerCount > 0 && iStoredPlayers > 0)
            {
                float tempHSPercent = 0F;
                foreach (KeyValuePair<string, CheaterAlert.PlayerStats> player in this.m_dicPlayerData)
                {
                    tempHSPercent += player.Value.CalculateHSPercent();
                }
                if (tempHSPercent > 0)
                {
                    this.m_fAverageHSPercent = (float)tempHSPercent / (float)iStoredPlayers;
                    return this.m_fAverageHSPercent;
                }
                else
                {
                    return 0F;
                }

            }
            else
            {
                return 0F;
            }
        }

        // calculates the average kills per minute
        public float AverageKPM()
        {
            int iStoredPlayers = this.m_dicPlayerData.Count;
            if (this.m_iPlayerCount > 0 && iStoredPlayers > 0)
            {
                float tempKPM = 0F;
                foreach (KeyValuePair<string, CheaterAlert.PlayerStats> player in this.m_dicPlayerData)
                {
                    tempKPM += player.Value.CalculateKPM();
                }
                this.m_fAverageKPM = (float)tempKPM / (float)iStoredPlayers;
                return this.m_fAverageKPM;
            }
            else
            {
                return 0F;
            }
        }

        // just created so I don't have to call all methods 
        public void CalculateAverages()
        {
            AverageKDR();
            AverageKills();
            AverageDeaths();
            AverageHSPercent();
            AverageKPM();
        }

        #endregion

        #region Player-checking

        // checks whether the KDR of a player is greater than the average and/or a given maximum
        public void CheckIngameKDR(string name)
        {
            double kdr = this.m_dicPlayerData[name].CalculateKDR();
            double averagekdr = 2 * (AverageKDR());
            double maxkdr = this.m_dIngameMaxKDR;
            if (kdr > averagekdr)
            {
                this.m_dicPlayerData[name].SuspectednessKDR += 1;
            }
            if (kdr > maxkdr)
            {
                this.m_dicPlayerData[name].SuspectednessKDR += 2;
            }
            if (this.m_dicPlayerData[name].SuspectednessKDR > 0 && this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Name: " + name + " SuspectednessKDR: " + this.m_dicPlayerData[name].SuspectednessKDR);
            }
        }

        // checks whether the number of kills of a player is greater than the average
        public void CheckIngameKills(string name)
        {
            int kills = this.m_dicPlayerData[name].Kills;
            double averageKills = 2 * (AverageKills());
            int maxkills = this.m_iIngameMaxKills;
            if (kills > averageKills)
            {
                this.m_dicPlayerData[name].SuspectednessKills += 1;
            }
            if (kills > maxkills)
            {
                this.m_dicPlayerData[name].SuspectednessKills += 2;
            }
            if (this.m_dicPlayerData[name].SuspectednessKills > 0 && this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Name: " + name + " SuspectednessKills: " + this.m_dicPlayerData[name].SuspectednessKills);
            }
        }

        // checks whether the percentage of headshots of a player is greater than a given maximum
        public void CheckIngameHSPercent(string name)
        {
            double hspercent = this.m_dicPlayerData[name].CalculateHSPercent();
            double averagehspercent = 2 * (AverageHSPercent());
            double maxhspercent = this.m_dIngameMaxHSPercent;
            if (hspercent > averagehspercent)
            {
                this.m_dicPlayerData[name].SuspectednessHSPercent += 1;
            }
            if (hspercent > maxhspercent)
            {
                this.m_dicPlayerData[name].SuspectednessHSPercent += 2;
            }
            if (this.m_dicPlayerData[name].SuspectednessHSPercent > 0 && this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Name: " + name + " SuspectednessHSPercent: " + this.m_dicPlayerData[name].SuspectednessHSPercent);
            }
        }

        // checks whether the KPM of a player are greater than a given maximum
        public void CheckIngameKPM(string name)
        {
            double kpm = this.m_dicPlayerData[name].CalculateKPM();
            double averagekpm = 2 * (AverageKPM());
            double maxkpm = this.m_dIngameMaxKPM;
            if (kpm > averagekpm)
            {
                this.m_dicPlayerData[name].SuspectednessKPM += 1;
            }
            if (kpm > maxkpm)
            {
                this.m_dicPlayerData[name].SuspectednessKPM += 2;
            }
            if (this.m_dicPlayerData[name].SuspectednessKPM > 0 && this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Name: " + name + " SuspectednessKPM: " + this.m_dicPlayerData[name].SuspectednessKPM);
            }
        }

        // checks whether the global KDR of a player is greater than a given maximum
        public void CheckGlobalKDR(string name)
        {
            // just trying to check the global stats if the playerdata-dictionary contains the needed entry and
            // the player's global stats haven't been flagged as missing before
            if (this.m_dicPlayerData.ContainsKey(name) == true && this.m_dicPlayerData[name].noGlobalStats == false)
            {
                // trying to retrieve stats if they haven't been fetched or there has been some error
                if (this.m_dicPlayerData[name].GlobalStats.retrieved == 0 || this.m_dicPlayerData[name].GlobalStats.failed == true || this.m_dicPlayerData[name].GlobalStats.valid == false)
                {
                    // trying to retrieve stats 3 times, flagging the global stats as missing if this limit is exeeded
                    if (this.m_dicPlayerData[name].GlobalStats.retrieved <= 3)
                    {
                        new Thread(RetrieveGlobalStats).Start(name);
                    }
                    else
                    {
                        this.ConsoleWrite("Global stats for " + name + " could not be retrieved! Please check the player yourself!");
                        if (this.m_ShowSysTrayAlert == enumBoolYesNo.Yes)
                        {
                            this.SysTrayAlert("No global stats for " + name, "true");
                        }
                        this.m_dicPlayerData[name].GlobalStats.noGlobalStats = true;
                    }
                }

                if (this.m_dicPlayerData[name].GlobalStats.noGlobalStats == false)
                {
                    // just continuing if the stats have been retrieved correctly, restarting the fetching if not
                    if (this.m_dicPlayerData[name].GlobalStats.failed == false && this.m_dicPlayerData[name].GlobalStats.valid == true)
                    {
                        if (this.m_dicPlayerData[name].GlobalStats.kdr > this.m_dGlobalMaxKDR)
                        {
                            this.m_dicPlayerData[name].SuspectednessGlobalKDR = true;
                            string tmpStatus = this.m_dicPlayerData[name].CheckStatus();
                            if (tmpStatus.CompareTo("Take Action") == 0)
                            {
                                if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                {
                                    this.IngameSayAll(String.Format("Cheater detected: '{0}'", name));
                                }
                                if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                {
                                    this.ConsoleWrite(String.Format("Cheater detected: '{0}'", name));
                                }
                                if (this.m_ShowSysTrayAlert == enumBoolYesNo.Yes)
                                {
                                    this.SysTrayAlert(String.Format("Cheater detected: '{0}'", name), "false");
                                }
                                if (this.m_dicPlayerData[name].AlreadyAdded == false)
                                {
                                    this.m_dicDetectedPlayers.Add(this.m_iNumberOfDetectedPlayers, name);
                                    this.m_iNumberOfDetectedPlayers++;
                                    this.m_dicPlayerData[name].AlreadyAdded = true;
                                }
                                PlayerDetected();

                                if (this.m_RemovePlayer == enumBoolYesNo.Yes)
                                {
                                    TakeAction(name);
                                }
                            }
                        }
                    }
                    else if (this.m_dicPlayerData[name].GlobalStats.failed == false && this.m_dicPlayerData[name].GlobalStats.valid == false)
                    {
                        CheckGlobalKDR(name);
                    }
                }
            }
        }

        #endregion

        // takes some action after successfully detecting a cheater. The action depends on the choices of the admin
        // oh btw... Yes, I looooove interlaced if-conditions and curly brackets :-D
        public void TakeAction(string name)
        {
            string tmpStatus = this.m_dicPlayerData[name].CheckStatus();
            if (tmpStatus.CompareTo("Take Action") == 0)
            {
                if (this.m_RemovePlayer == enumBoolYesNo.Yes)
                {
                    if (this.m_strRemoveOption == "Kick")
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", name, this.m_strKickReason);
                        if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                        {
                            this.IngameSayAll(String.Format("Player '{0}' kicked!", name));
                        }
                        if (this.m_SpamConsole == enumBoolYesNo.Yes)
                        {
                            this.ConsoleWrite(String.Format("Player '{0}' kicked!", name));
                        }
                    }
                    else if (this.m_strRemoveOption == "Temporary Ban" || this.m_strRemoveOption == "Permanent Ban")
                    {
                        string strBanReason = this.m_strBanReason;
                        strBanReason = strBanReason.Replace("%bt%", this.m_iBanTime.ToString());
                        if (this.m_strRemoveOption == "Temporary Ban")
                        {
                            if (this.m_strBanType == "Name")
                            {
                                this.ExecuteCommand("procon.protected.send", "banList.add", "name", name, "seconds", (this.m_iBanTime * 60).ToString(), strBanReason);
                                this.ExecuteCommand("procon.protected.send", "banList.save");
                                this.ExecuteCommand("procon.protected.send", "banList.list");
                                if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                {
                                    this.IngameSayAll(String.Format("Player '{0}' banned for {1} minutes!", name, this.m_iBanTime));
                                }
                                if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                {
                                    this.ConsoleWrite(String.Format("Player '{0}' banned for {1} minutes!", name, this.m_iBanTime));
                                }
                            }
                            else if (this.m_strBanType == "GUID")
                            {
                                if (this.m_dicPlayerGUID.ContainsKey(name) == true)
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_dicPlayerGUID[name], "seconds", (this.m_iBanTime * 60).ToString(), strBanReason);
                                    this.ExecuteCommand("procon.protected.send", "banList.save");
                                    this.ExecuteCommand("procon.protected.send", "banList.list");
                                    if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                    {
                                        this.IngameSayAll(String.Format("Player '{0}' banned for {1} minutes.", name, this.m_iBanTime));
                                    }
                                    if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                    {
                                        this.ConsoleWrite(String.Format("Player '{0}' banned for {1} minutes! GUID: {2}", name, this.m_iBanTime, this.m_dicPlayerGUID[name]));
                                    }
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", name, "seconds", (this.m_iBanTime * 60).ToString(), strBanReason);
                                    this.ExecuteCommand("procon.protected.send", "banList.save");
                                    this.ExecuteCommand("procon.protected.send", "banList.list");
                                    if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                    {
                                        this.IngameSayAll(String.Format("Player '{0}' banned for {1} minutes.", name, this.m_iBanTime));
                                    }
                                    if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                    {
                                        this.ConsoleWrite(String.Format("I don't have the BC2-GUID of '{0}' stored. Banning him for {1} minutes using his name!", name, this.m_iBanTime));
                                    }
                                }
                            }
                        }
                        else if (this.m_strRemoveOption == "Permanent Ban")
                        {
                            if (this.m_strBanType == "Name")
                            {
                                this.ExecuteCommand("procon.protected.send", "banList.add", "name", name, "perm", this.m_strPermBanReason);
                                this.ExecuteCommand("procon.protected.send", "banList.save");
                                this.ExecuteCommand("procon.protected.send", "banList.list");
                                if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                {
                                    this.IngameSayAll(String.Format("Player '{0}' banned permanently!", name));
                                }
                                if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                {
                                    this.ConsoleWrite(String.Format("Player '{0}' banned permanently!", name));
                                }
                            }
                            else if (this.m_strBanType == "GUID")
                            {
                                if (this.m_PBGUIDBan == enumBoolYesNo.No)
                                {
                                    if (this.m_dicPlayerGUID.ContainsKey(name) == true)
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_dicPlayerGUID[name], "perm", this.m_strPermBanReason);
                                        this.ExecuteCommand("procon.protected.send", "banList.save");
                                        this.ExecuteCommand("procon.protected.send", "banList.list");
                                        if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                        {
                                            this.IngameSayAll(String.Format("Player '{0}' banned permanently!", name));
                                        }
                                        if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                        {
                                            this.ConsoleWrite(String.Format("Player '{0}' banned permanently! GUID: {1}", name, this.m_dicPlayerGUID[name]));
                                        }
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", name, "perm", this.m_strPermBanReason);
                                        this.ExecuteCommand("procon.protected.send", "banList.save");
                                        this.ExecuteCommand("procon.protected.send", "banList.list");
                                        if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                        {
                                            this.IngameSayAll(String.Format("Player '{0}' banned permanently!", name));
                                        }
                                        if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                        {
                                            this.ConsoleWrite(String.Format("I don't have the BC2-GUID of '{0}' stored. Banning him permanently using his name!", name));
                                        }
                                    }
                                }
                                else if (this.m_PBGUIDBan == enumBoolYesNo.Yes)
                                {
                                    if (this.m_dicPlayerPBGUID.ContainsKey(name) == true)
                                    {
                                        this.PBCommand(String.Format("pb_sv_banguid \"{0}\" \"{1}\" \"{2}\" \"{3}\"", this.m_dicPlayerPBGUID[name], name, this.m_dicPlayerPunkbusterInfo[name].Ip, "BC2! " + this.m_strPermBanReason));
                                    }
                                    else if (this.m_dicPlayerPunkbusterInfo.ContainsKey(name) == true)
                                    {
                                        this.PBCommand(String.Format("pb_sv_banguid \"{0}\" \"{1}\" \"{2}\" \"{3}\"", this.m_dicPlayerPunkbusterInfo[name].GUID, name, this.m_dicPlayerPunkbusterInfo[name].Ip, "BC2! " + this.m_strPermBanReason));
                                        this.PBCommand(String.Format("pb_sv_kick \"{0}\" \"{1}\" \"{2}\" \"{3}\"", this.m_dicPlayerPunkbusterInfo[name].SlotID, 1337, "BC2! " + this.m_strPermBanReason, name + " banned by CheaterAlert by MorpheusX(AUT)"));
                                    }
                                    this.ExecuteCommand("procon.protected.send", "banList.save");
                                    this.ExecuteCommand("procon.protected.send", "banList.list");
                                    if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                                    {
                                        this.IngameSayAll(String.Format("Player '{0}' banned permanently!", name));
                                    }
                                    if (this.m_SpamConsole == enumBoolYesNo.Yes)
                                    {
                                        this.ConsoleWrite(String.Format("Player '{0}' banned permanently! PB-GUID: {1}", name, this.m_dicPlayerPBGUID[name]));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Email Notifications

        // planned: including an email-notification system, sending mails if a cheater is detected

        #endregion

        #region SQL methods

        // sending some averages and maxima to the mySQL-database
        public void SendServerStats()
        {
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Starting to send data to database");
                }
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                           "SERVER=" + m_strSQLHostname + ";" +
                                                           "PORT=3306;" +
                                                           "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                           "UID=" + m_strSQLUsername + ";" +
                                                           "PWD=" + m_strSQLPassword + ";" +
                                                           "OPTION=3;");

                    OdbcCon.Open();

                    this.ConsoleDebug("Successfully connected. Start sending serverstats");

                    string insertSQL = "INSERT INTO tbl_serverstats (serverName, serverAddress, playerCount, map, averageKills, averageDeaths, averageKDR, averageHSPercent, averageKPM, highestKills, highestDeaths, highestKDR, highestHSPercent, highestKPM, ingameMaxKills, ingameMaxKDR, ingameMaxHSPercent, globalMaxKDR, datetime) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";

                    using (OdbcCommand OdbcCom = new OdbcCommand(insertSQL, OdbcCon))
                    {
                        OdbcCom.Parameters.AddWithValue("@pr", m_strServerName);
                        OdbcCom.Parameters.AddWithValue("@pr", m_strServerAddress);
                        OdbcCom.Parameters.AddWithValue("@pr", m_iPlayerCount);
                        OdbcCom.Parameters.AddWithValue("@pr", m_strCurrentMap);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fAverageKills);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fAverageDeaths);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fAverageKDR);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fAverageHSPercent);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fAverageKPM);
                        OdbcCom.Parameters.AddWithValue("@pr", m_iHighestKills);
                        OdbcCom.Parameters.AddWithValue("@pr", m_iHighestDeaths);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fHighestKDR);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fHighestHSPercent);
                        OdbcCom.Parameters.AddWithValue("@pr", m_fHighestKPM);
                        OdbcCom.Parameters.AddWithValue("@pr", m_iIngameMaxKills);
                        OdbcCom.Parameters.AddWithValue("@pr", m_dIngameMaxKDR);
                        OdbcCom.Parameters.AddWithValue("@pr", m_dIngameMaxHSPercent);
                        OdbcCom.Parameters.AddWithValue("@pr", m_dGlobalMaxKDR);
                        OdbcCom.Parameters.AddWithValue("@pr", DateTime.Now);

                        OdbcCom.ExecuteNonQuery();
                    }

                    this.ConsoleDebug("Finished sending serverstats");
                }

                catch (Exception c)
                {
                    this.ConsoleWrite("Error: " + c);
                }
            }

            else
            {
                this.ConsoleWrite("Error: Did you alter something within the sourcecode? Nahnahnah, don't do that! Databaseconnection won't work otherwise!");
            }

            if (OdbcCon.State == ConnectionState.Open)
            {
                OdbcCon.Close();
            }
            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Finished sending data to database");
            }
        }

        // sending the list of detected players and their stats to the database
        public void SendDetectedPlayers()
        {
            this.m_blSendingPlayers = true;
            if ((m_strSQLHostname != null) || (m_strSQLDatabaseName != null) || (m_strSQLUsername != null) || (m_strSQLPassword != null))
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Starting to send data to database");
                }
                try
                {
                    OdbcParameter param = new OdbcParameter();

                    OdbcConn = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                           "SERVER=" + m_strSQLHostname + ";" +
                                                           "PORT=3306;" +
                                                           "DATABASE=" + m_strSQLDatabaseName + ";" +
                                                           "UID=" + m_strSQLUsername + ";" +
                                                           "PWD=" + m_strSQLPassword + ";" +
                                                           "OPTION=3;");

                    OdbcConn.Open();

                    this.ConsoleDebug("Successfully connected. Start sending stats of detected players");

                    for (int i = 0; i < this.m_iNumberOfDetectedPlayers; i++)
                    {
                        if (this.m_dicDetectedPlayers.ContainsKey(i) == true)
                        {
                            string name = this.m_dicDetectedPlayers[i].ToString();
                            if (this.m_dicPlayerData.ContainsKey(name) == true && this.m_dicPlayerData[name].AlreadySent == false)
                            {
                                this.ConsoleDebug("Sending stats of " + name);
                                string insertSQL = "INSERT INTO tbl_detectedplayers (playerName, clanTag, playerGUID, playerPBGUID, playerKills, playerDeaths, playerKDR, playerHSPercent, playerKPM, status, playerSuspectedness, playerSuspectednessKDR, playerSuspectednessKPM, playerSuspectednessHSPercent, playerSuspectednessKills, serverName, serverAddress, playerCount, map, datetime) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";

                                using (OdbcCommand OdbcComm = new OdbcCommand(insertSQL, OdbcConn))
                                {
                                    OdbcComm.Parameters.AddWithValue("@pr", name);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].ClanTag);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerGUID[name]);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerPBGUID[name]);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].Kills);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].Deaths);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].CalculateKDR());
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].CalculateHSPercent());
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].CalculateKPM());
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].CheckStatus());
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].Suspectedness);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].SuspectednessKDR);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].SuspectednessKPM);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].SuspectednessHSPercent);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_dicPlayerData[name].SuspectednessKills);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_strServerName);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_strServerAddress);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_iPlayerCount);
                                    OdbcComm.Parameters.AddWithValue("@pr", m_strCurrentMap);
                                    OdbcComm.Parameters.AddWithValue("@pr", DateTime.Now);

                                    OdbcComm.ExecuteNonQuery();
                                }

                                this.m_dicPlayerData[name].AlreadySent = true;
                            }
                        }
                    }

                    this.ConsoleDebug("Finished sending detected players");
                }

                catch (Exception c)
                {
                    this.ConsoleWrite("Error: " + c);
                    this.m_blSendingPlayers = false;
                }
            }

            else
            {
                this.ConsoleWrite("Error: Did you alter something within the sourcecode? Nahnahnah, don't do that! Databaseconnection won't work otherwise!");
            }

            if (OdbcConn.State == ConnectionState.Open)
            {
                OdbcConn.Close();
            }
            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Finished sending data to database");
            }
            this.m_blSendingPlayers = false;
        }

        #endregion

        #region PBBans MBI

        // initiates a new download of the PBBans MBI
        public void DownloadMBI()
        {
            if (this.m_blMBIDownload == false)
            {
                while (this.m_blMBISearch == true)
                {
                    Thread.Sleep(5000);
                }

                this.m_blMBIDownload = true;
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Attempting to download the latest PBBans MBI");
                }
                try
                {
                    this.m_cdfMBIDownloader = new CDownloadFile("http://www.pbbans.com/mbi-download-bfbc2-dlb36.html");
                    this.m_cdfMBIDownloader.BeginDownload();
                    this.m_cdfMBIDownloader.DownloadComplete += new CDownloadFile.DownloadFileEventDelegate(CDownloadFile_DownloadComplete);
                }
                catch (Exception e)
                {
                    this.ConsoleDebug("Error while downloading PBBans MBI: " + e);
                    this.m_blMBIDownload = false;
                }
            }
        }

        // fired when the download is completed, saves downloaded file to harddisk
        private void CDownloadFile_DownloadComplete(CDownloadFile sender)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(@"Plugins/BFBC2/CheaterAlert/PBBans");
                if (!di.Exists)
                {
                    di.Create();
                }
                System.IO.File.WriteAllBytes("Plugins/BFBC2/CheaterAlert/PBBans/mbi.dat", sender.CompleteFileData);
            }
            catch (Exception e)
            {
                this.ConsoleDebug("Error while saving PBBans MBI: " + e);
                this.m_blMBIDownload = false;
            }

            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Finished downloading the latest PBBans MBI");
            }
            this.m_blMBIDownload = false;
        }

        // method used to search the PBBans MBI for a name
        public void SearchMBI(object soldier)
        {
            FileInfo fi = new FileInfo("Plugins/BFBC2/CheaterAlert/PBBans/mbi.dat");
            if (!fi.Exists)
            {
                new Thread(DownloadMBI).Start();
                Thread.Sleep(5000);
            }

            string SoldierName = soldier.ToString();
            while (this.m_blMBIDownload == true)
            {
                Thread.Sleep(5000);
            }

            this.m_blMBISearch = true;
            string line = String.Empty;
            string name = "\"" + SoldierName + "\"";

            using (StreamReader reader = new StreamReader("Plugins/BFBC2/CheaterAlert/PBBans/mbi.dat"))
            {
                for (int i = 0; i < 8; i++)
                {
                    line = reader.ReadLine();
                }
                while ((line = reader.ReadLine()) != null)
                {
                    string[] split = line.Split(new Char[] { ' ' });
                    if (split[3].CompareTo(name) == 0)
                    {
                        if (this.m_SpamConsole == enumBoolYesNo.Yes)
                        {
                            this.ConsoleWrite(String.Format("Player '{0}' found on PBBans' MBI. Kicking.", SoldierName));
                        }
                        if (this.m_IngameNotifications == enumBoolYesNo.Yes)
                        {
                            this.IngameSayAll(String.Format("Player '{0}' found on PBBans' MBI. Kicking.", SoldierName));
                        }
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName, "Your soldiername was found on PBBans' Master Ban Index. Goodbye!");
                    }
                }
            }
            this.m_blMBISearch = false;
        }

        #endregion

        #endregion

        #region Classes & Structs

        #region PlayerStats

        // "mighty" class :-D
        // storing nearly all data about a player
        private class PlayerStats
        {
            public string SoldierName;
            public string ClanTag;
            public int Kills;
            public int Deaths;
            public float KDR;
            public int Headshots;
            public float HSPercent;
            public string Kit;
            public string Weapon;
            public int sumKPM;
            public int numKPM;
            public float KPM;
            public float SPM;
            public int Suspectedness;
            public int SuspectednessKDR;
            public int SuspectednessKPM;
            public int SuspectednessHSPercent;
            public int SuspectednessKills;
            public bool SuspectednessGlobalKDR;
            public string Status;
            public bool AlreadyAdded;
            public bool AlreadySent;
            public bool noGlobalStats;
            public GlobalPlayerStats GlobalStats;

            public PlayerStats(string Name, string Tag)
            {
                this.SoldierName = Name;
                this.ClanTag = Tag;
                this.Kills = 0;
                this.Deaths = 0;
                this.KDR = 0F;
                this.Headshots = 0;
                this.Kit = "";
                this.Weapon = "";
                this.sumKPM = 0;
                this.numKPM = 0;
                this.KPM = 0F;
                this.SPM = 0F;
                this.Suspectedness = 0;
                this.SuspectednessKDR = 0;
                this.SuspectednessKPM = 0;
                this.SuspectednessHSPercent = 0;
                this.SuspectednessKills = 0;
                this.SuspectednessGlobalKDR = false;
                this.Status = "Low";
                this.AlreadyAdded = false;
                this.AlreadySent = false;
                this.noGlobalStats = false;
                this.GlobalStats = new GlobalPlayerStats();
                this.GlobalStats.initialize();
            }

            public float CalculateKDR()
            {
                if (this.Deaths != 0)
                {
                    this.KDR = (float)this.Kills / (float)this.Deaths;
                }
                else
                {
                    this.KDR = (float)this.Kills;
                }
                return this.KDR;
            }

            public float CalculateHSPercent()
            {
                if (this.Headshots > 0 && this.Kills > 0)
                {
                    this.HSPercent = (float)(this.Headshots * 100) / (float)this.Kills;
                    return this.HSPercent;
                }
                else
                {
                    return 0F;
                }
            }

            public float CalculateKPM()
            {
                if (this.numKPM > 0)
                {
                    this.KPM = (float)this.sumKPM / (float)this.numKPM;
                    return this.KPM;
                }
                else
                {
                    return 0F;
                }

            }

            public void Reset()
            {
                this.Kills = 0;
                this.Deaths = 0;
                this.KDR = 0F;
                this.Headshots = 0;
                this.Kit = "";
                this.Weapon = "";
                this.KPM = 0F;
                this.sumKPM = 0;
                this.numKPM = 0;
                this.SPM = 0F;
            }

            public string CheckStatus()
            {
                if (this.SuspectednessKDR > 3)
                {
                    this.Status = "Watch";
                }
                if (this.SuspectednessKills > 3)
                {
                    this.Status = "Elevated";
                }
                if (this.SuspectednessHSPercent > 3)
                {
                    this.Status = "High";
                }
                if (this.SuspectednessKPM > 3)
                {
                    this.Status = "Severe";
                }
                if (this.SuspectednessGlobalKDR == true)
                {
                    this.Status = "Take Action";
                }

                return this.Status;
            }
        }

        #endregion

        #region GlobalPlayerStats

        // struct to save a player's global stats
        private struct GlobalPlayerStats
        {
            public bool failed;
            public bool valid;
            public bool noGlobalStats;
            public int retrieved;
            public int kills;
            public int deaths;
            public float kdr;

            public void initialize()
            {
                failed = false;
                valid = true;
                noGlobalStats = false;
                retrieved = 0;
                kills = 0;
                deaths = 0;
                kdr = 0F;
            }

            public void reset()
            {
                failed = false;
                valid = true;
                retrieved = 0;
                kills = 0;
                deaths = 0;
                kdr = 0F;
            }
        }

        // actual method to retrieve stats from BFBCS.com
        private void getBFBCSStats(string name)
        {
            try
            {
                // creating a new webclient to retrieve stats
                WebClient wc = new WebClient();
                string address = "http://api.bfbcs.com/api/pc?players=" + name.Replace("&", "%26") + "&fields=basic";
                string result = wc.DownloadString(address);

                // decoding the retrieved JSON-data
                Hashtable data = (Hashtable)JSON.JsonDecode(result);

                double found;
                // checking whether stats could be found withing BFBCS's database
                if (!(data.Contains("found") && Double.TryParse(data["found"].ToString(), out found) == true && found == 1))
                {
                    this.m_dicPlayerData[name].GlobalStats.failed = true;
                }
                else if (data.Contains("found") && Double.TryParse(data["found"].ToString(), out found) == true && found == 1)
                {
                    if (this.m_dicPlayerData.ContainsKey(name) == false)
                    {
                        this.m_dicPlayerData.Add(name, new PlayerStats(name, ""));
                    }

                    // interpreting the fetched results
                    Hashtable playerData = (Hashtable)((ArrayList)data["players"])[0];

                    int.TryParse(playerData["kills"].ToString(), out this.m_dicPlayerData[name].GlobalStats.kills);
                    int.TryParse(playerData["deaths"].ToString(), out this.m_dicPlayerData[name].GlobalStats.deaths);
                    if (this.m_dicPlayerData[name].GlobalStats.deaths > 0)
                    {
                        this.m_dicPlayerData[name].GlobalStats.kdr = (float)this.m_dicPlayerData[name].GlobalStats.kills / (float)this.m_dicPlayerData[name].GlobalStats.deaths;
                    }
                    else
                    {
                        this.m_dicPlayerData[name].GlobalStats.kdr = (float)this.m_dicPlayerData[name].GlobalStats.kills;
                    }
                    this.m_dicPlayerData[name].GlobalStats.retrieved += 1;
                    this.m_dicPlayerData[name].GlobalStats.failed = false;
                }
            }
            catch (Exception e)
            {
                // exception occurred while requesting stats
                this.ConsoleDebug("Error: " + e);
                this.m_dicPlayerData[name].GlobalStats.failed = true;
            }
        }

        // method executed by the stats-retrieving thread
        public void RetrieveGlobalStats(object name)
        {
            string playername = name.ToString();
            if (this.m_DebugMode == enumBoolYesNo.Yes)
            {
                this.ConsoleDebug("Start retrieving global stats for " + playername);
            }

            // getting stats
            getBFBCSStats(playername);

            if (this.m_dicPlayerData[playername].GlobalStats.failed == true)
            {
                if (this.m_DebugMode == enumBoolYesNo.Yes)
                {
                    this.ConsoleDebug("Failed retrieving global stats for " + playername);
                }
                this.m_dicPlayerData[playername].GlobalStats.retrieved += 1;
                this.m_dicPlayerData[playername].GlobalStats.valid = false;
            }
            else
            {
                // checking whether the retrieved KDR is greater than 0, taking it as an indicator for valid stats
                if (this.m_dicPlayerData[playername].GlobalStats.kdr > 0)
                {
                    if (this.m_DebugMode == enumBoolYesNo.Yes)
                    {
                        this.ConsoleDebug("Successfully retrieved global stats for " + playername);
                    }
                    this.m_dicPlayerData[playername].GlobalStats.retrieved += 1;
                    this.m_dicPlayerData[playername].GlobalStats.valid = true;
                }
                else
                {
                    this.m_dicPlayerData[playername].GlobalStats.valid = false;
                }
            }
        }

        #endregion

        #endregion

        #region Unused Interfaces

        /*
        public void OnAccountCreated(string strUsername)
        {

        }

        public void OnAccountDeleted(string strUsername)
        {

        }

        public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges cpPrivs)
        {

        }

        public void OnReceiveProconVariable(string strVariableName, string strValue)
        {

        }

        public void OnConnectionClosed()
        {

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
            
        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {

        }

        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {

        }

        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

        }

        public void OnLogin()
        {

        }

        public void OnLogout()
        {

        }

        public void OnQuit()
        {

        }

        public void OnVersion(string strServerType, string strVersion)
        {

        }

        public void OnHelp(List<string> lstCommands)
        {

        }

        public void OnRunScript(string strScriptFileName)
        {

        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {

        }

        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }

        public void OnCurrentLevel(string strCurrentLevel)
        {

        }

        public void OnSetNextLevel(string strNextLevel)
        {

        }

        public void OnRestartLevel()
        {

        }

        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }

        public void OnPlayerKicked(string strSoldierName, string strReason)
        {

        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnBanList(List<CBanInfo> lstBans)
        {

        }

        public void OnBanAdded(CBanInfo cbiBan)
        {

        }

        public void OnBanRemoved(CBanInfo cbiUnban)
        {

        }

        public void OnBanListClear()
        {

        }

        public void OnBanListLoad()
        {

        }

        public void OnBanListSave()
        {

        }

        public void OnReservedSlotsConfigFile(string strConfigFilename)
        {

        }

        public void OnReservedSlotsLoad()
        {

        }

        public void OnReservedSlotsSave()
        {

        }

        public void OnReservedSlotsPlayerAdded(string strSoldierName)
        {

        }

        public void OnReservedSlotsPlayerRemoved(string strSoldierName)
        {

        }

        public void OnReservedSlotsCleared()
        {

        }

        public void OnReservedSlotsList(List<string> lstSoldierNames)
        {

        }

        public void OnMaplistConfigFile(string strConfigFilename)
        {

        }

        public void OnMaplistLoad()
        {

        }

        public void OnMaplistSave()
        {

        }

        public void OnMaplistMapAppended(string strMapFileName)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }

        public void OnGamePassword(string strGamePassword)
        {

        }

        public void OnPunkbuster(bool blEnabled)
        {

        }

        public void OnHardcore(bool blEnabled)
        {

        }

        public void OnRanked(bool blEnabled)
        {

        }

        public void OnRankLimit(int iRankLimit)
        {

        }

        public void OnTeamBalance(bool blEnabled)
        {

        }

        public void OnFriendlyFire(bool blEnabled)
        {

        }

        public void OnMaxPlayerLimit(int iMaxPlayerLimit)
        {

        }

        public void OnCurrentPlayerLimit(int iCurrentPlayerLimit)
        {

        }

        public void OnPlayerLimit(int iPlayerLimit)
        {

        }

        public void OnBannerURL(string strURL)
        {

        }

        public void OnServerDescription(string strServerDescription)
        {

        }

        public void OnKillCam(bool blEnabled)
        {

        }

        public void OnMiniMap(bool blEnabled)
        {

        }

        public void OnCrossHair(bool blEnabled)
        {

        }

        public void On3dSpotting(bool blEnabled)
        {

        }

        public void OnMiniMapSpotting(bool blEnabled)
        {

        }

        public void OnThirdPersonVehicleCameras(bool blEnabled)
        {

        }

        public void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {

        }

        public void OnServerName(string strServerName)
        {

        }

        public void OnTeamKillCountForKick(int iLimit)
        {

        }

        public void OnTeamKillValueIncrease(int iLimit)
        {

        }

        public void OnTeamKillValueDecreasePerSecond(int iLimit)
        {

        }

        public void OnTeamKillValueForKick(int iLimit)
        {

        }

        public void OnIdleTimeout(int iLimit)
        {

        }

        public void OnProfanityFilter(bool isEnabled)
        {

        }

        public void OnRoundOverPlayers(List<string> lstPlayers)
        {

        }

        public void OnEndRound(int iWinningTeamID)
        {

        }

        public void OnLevelVariablesList(LevelVariable lvRequestedContext, List<LevelVariable> lstReturnedValues)
        {

        }

        public void OnLevelVariablesEvaluate(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        public void OnLevelVariablesClear(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesSet(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesGet(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        public void OnAnyMatchRegisteredCommand(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage)
        {

        }

        public void OnRegisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnUnregisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {

        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState)
        {

        }
        */

        #endregion

    }
}