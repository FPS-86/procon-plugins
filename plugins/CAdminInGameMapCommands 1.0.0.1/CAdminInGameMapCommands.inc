/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.

 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents {
    public class CAdminInGameMapCommands : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        
        private string m_strGameType;
        private string m_strGameServerType;

        private string m_strPrivatePrefix;
        private string m_strAdminsPrefix;
        private string m_strPublicPrefix;

        private string m_strConfirmCommand;
        private string m_strCancelCommand;

        private string m_strMapCommand;
        private string m_strMapListCommand;
        private string m_strShowNextMapCommand;
        private string m_strGameModeCommand;

        enumBoolYesNo m_enDoConsoleOutput;
        enumBoolYesNo m_enDoDebugOutput;
        enumBoolYesNo m_enDoQuietMode;
        enumBoolYesNo m_enDoEnforceComReg;
        enumBoolYesNo m_enDoSaveNewMaplist;

        enumBoolYesNo m_enDoMapRestartOnZero;

        private GamemodeInformationDictionary m_gameModeSettings;
        private string m_strSelectedGamemode;
        private string m_strNxtPlayList;

        private List<MaplistEntry> m_lstCurMapList;
        private string m_strCurMapFileName;
        private int m_iCurMapIndex;
        private int m_iNextMapIndex;
        private int m_iNextMapIndexOld;
        
        private bool m_blNextMapNew;
        private bool m_blNewRoundStarted;

        private string[] m_aryGameModeList;
        private string m_strCurGameMode;
        private string m_strNxtGameMode;
        private string m_strAddMapMethod;

        CServerInfo m_csiLatestServerInfo;
        private bool m_blFinalRoundSet;
        private int m_iCurMapRounds;
        private int m_iCurMapMaxRounds;

        private bool m_blReadyToRestart;

        private bool m_isPluginEnabled;

        public CAdminInGameMapCommands()
        {
            this.m_strGameType = string.Empty;

            this.m_strPrivatePrefix = "@";
            this.m_strAdminsPrefix = "#";
            this.m_strPublicPrefix = "!";

            this.m_strConfirmCommand = "myes";
            this.m_strCancelCommand = "mno";
            
            this.m_strMapCommand = "map";
            this.m_strMapListCommand = "rotation";
            this.m_strShowNextMapCommand = "shownextmap";
            this.m_strGameModeCommand = "gamemode";

            this.m_enDoConsoleOutput = enumBoolYesNo.No;
            this.m_enDoDebugOutput = enumBoolYesNo.No;
            this.m_enDoQuietMode = enumBoolYesNo.No;
            this.m_enDoEnforceComReg = enumBoolYesNo.No;
            this.m_enDoSaveNewMaplist = enumBoolYesNo.No;

            this.m_enDoMapRestartOnZero = enumBoolYesNo.No;

            this.m_gameModeSettings = new GamemodeInformationDictionary();
            this.m_strSelectedGamemode = String.Empty;

            this.m_lstCurMapList = new List<MaplistEntry>();
            this.m_strCurMapFileName = "";
            this.m_iCurMapIndex = 0;
            this.m_iNextMapIndex = -1;
            this.m_iNextMapIndexOld = -1;
            
            this.m_blNextMapNew = false;
            this.m_blNewRoundStarted = false;

            this.m_strCurGameMode = string.Empty;
            this.m_strNxtGameMode = string.Empty;
            this.m_strAddMapMethod = "insert";

            this.m_csiLatestServerInfo = null;
            this.m_blFinalRoundSet = false;
            this.m_iCurMapRounds = 0;
            this.m_iCurMapMaxRounds = 0;

            this.m_blReadyToRestart = false;

            #include "CAdminInGameMapCommands_GameServer.inc"

            this.m_isPluginEnabled = false;
        }

        public string GetPluginName()
        {
            return "Admin InGame Map Commands";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.1";
        }

        public string GetPluginAuthor()
        {
            return "Phil_K";
        }

        public string GetPluginWebsite()
        {
            return "forum.myrcon.com/showthread.php?1856-Admin-InGame-Map-Commands";
        }

        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
    <p>Provides in-game commands for maplist manipulations. Those can be to define the next
    map to be run or to query available maps for the running gametype. It interacts with
    BasicInGameInfo & InGameAdmin.</p>

<h2>How it works</h2>
    <p>Being enabled the plugin will get a list of all available maps for all supported 
    gamemodes in the running mod (vanilla BFBC2 or BFBC2-Vietnam). Next it will check
    which is the current map and the gamemode. With this information it provides the list
    of available maps. Maps which should be selected are checked against maps in the
    running gamemode. If a selection is confirmed the plugin checks if that map is already
    in the current maplist rotation. On a hit the nextLevelIndex is set to that map in the
    rotation. If the selected map is not in the rotation it is instered into the maplist 
    rotation and the nextLevelIndex is set to that map. The placement is done regarding 
    the setting (add map method). A map is inserted with two rounds to play.</p>
    <p> You need to issue the InGameAdmin 'Next Map' command (nextlevel by default) to 
    forward to the selected map.</p>

<h2>Commands</h2>
    <blockquote><h4>@map</h4>
        <ul>
            <li>usable by all players</li>
            <li>will list all available maps in the running gamemode</li>
        </ul>
    </blockquote>
    <blockquote><h4>@map [MapName] [optional: fast]</h4>
        <ul>
            <li>only usable by admins (Procon account holder) with CanUseMapFunctions privileges</li>
            <li>[MapName] can be the level file name (Levels/mp_001sr) or the friendly name (Panama Canal). A short form can be used.</li>
            <li>[optional: fast] adding 'fast' to the command while pointing/searching a map filename will directly load the map.<br/>
            Note: You must select a map by the filename to use fast-option!</li>
            <li>sets the selected and confirmed map as the next map</li>
        </ul>
    </blockquote>
    <blockquote><h4>@rotation</h4>
        <ul>
            <li>usable by all players</li>
            <li>will list all maps in the current mapList/Rotation with id-nr, name and filename.</li>
        </ul>
    </blockquote>

    <blockquote><h4>@shownextmap</h4>
        <ul>
            <li>usable by all players</li>
            <li>will show which map will be next in the rotation. If the nextmap is another round the output will show that.</li>
        </ul>
    </blockquote>

    <blockquote><h4>@gamemode</h4>
        <ul>
            <li>usable by all players</li>
            <li>will list all available gamemode</li>
        </ul>
    </blockquote>
    <blockquote><h4>@gamemode [GameMode] [optional: mapFileName / all] [optional: fast] [optional: override]</h4>
        <ul>
            <li>only usable by admins (Procon account holder) with CanUseMapFunctions privileges</li>
            <li>[GameMode] has to be one of the keywords just like the server wants it for admin.setPlaylist command.<br/>
                (CONQUEST/RUSH/SQRUSH/SQDM)</li>
            <li>[mapFileName] can be the keyword 'all' to add all maps/levels of the selected gamemode to the new rotation.<br/>
                To add a specific map use the correct filename incl. 'Levels/' path.</li>
            <li>[fast] use this keyword to change to the new gamemode and map directly.</li>
            <li>[override] use this keyword to override the gamemode maxPlayer count.<br/>
                (CONQUEST & RUSH up to 32 players, SQDM up to 16 players, SQRUSH up to 8 players)</li>
            <li>sets the selected and confirmed gamemode if the actual player count fits the selection</li>
            <li>clears the maplist, adds all available maps for the selected gamemode and forwards to the first map in the selected gamemode.</li>
        </ul>
    </blockquote>

<h2>Settings</h2>
    <h3>Miscellaneous</h3>
        <blockquote><h4>Map command</h4>Defines the map command keyword.</blockquote>
        <blockquote><h4>MapList command</h4>Defines the command to prints the current mapList/Rotation.</blockquote>
        <blockquote><h4>Show next map command</h4>Defines the command to show the next map to be played.</blockquote>
        <blockquote><h4>Gamemode command</h4>Defines the gamemode command keyword.</blockquote>
        <blockquote><h4>Confirm command</h4>Defines the keyword to confirm selections.</blockquote>
        <blockquote><h4>Add map method</h4>Sets if a selected map which is not in the actual rotation should be inserted after the running map or to the end of the rotation.</blockquote>
        <blockquote><h4>Save maplist</h4>Select Yes to save all changes to the maplist to the maplist file and make it reboot save. This will also save a changed playlist/gamemode.</blockquote>
        <blockquote><h4>Restart on zero</h4>Restarts the current map as playercount hits zero.</blockquote>
        <blockquote><h4>Console output</h4>Prints status informations to the plugin console.</blockquote>
        <blockquote><h4>Debug output</h4>Prints status and debug information to the plugin console.</blockquote>

<br/>

<h2>Credits</h2>
    <blockquote><h4>Phogue</h4>
        <ul>        
            <li>for Procon</li>
            <li>Ploho League Controls, parts of it are used in here</li>
        </ul>
    </blockquote>
    <blockquote><h4>other plugin authors</h4>
        <ul><li>for all the plugins and code insperations</li></ul>
    </blockquote>
    <blockquote><h4>Procon users</h4>
        <ul><li>for your ideas, comments and feedback</li><ul>
    </blockquote>
<br/>

<h2>Development</h2>
    <h3>To-Do</h3>
        <blockquote><h4>Feedback</h4>
            - wait for feedback ;)<br/>
        </blockquote>

    <h3>Changelog</h3>
        <blockquote><h4>1.0.0.1 (2011-03-28)</h4>
            - fixed a problem with the random map on gamemode selection<br/>
        </blockquote>
        <blockquote><h4>1.0.0.0 (2011-03-27)</h4>
            - added Restart on Zero<br/>
            - finished GameMode selection commands
        </blockquote>
        <blockquote><h4>0.5.2.0 (2011-03-23)</h4>
            - added shownextmap command<br/>
        </blockquote>
        <blockquote><h4>0.5.1.0 (2011-03-19)</h4>
            - added fastforward option<br/>
        </blockquote>
        <blockquote><h4>0.5.0.0 (2011-03-19)</h4>
            - first public beta<br/>
        </blockquote>
        <blockquote><h4>0.0.0.1 (2011-02-19)</h4>
            - start of development<br/>
        </blockquote>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {

            //this.RegisterEvents(this.GetType().Name, "OnMaplistList", "OnMaplistLoad", "OnMaplistSave", "OnMaplistMapAppended", "OnMaplistMapRemoved", "OnMaplistMapInserted", "OnMaplistCleared", "OnPlaylistSet", "OnMaplistNextLevelIndex");
            this.RegisterEvents(this.GetType().Name, "OnServerInfo", "OnCurrentLevel", "OnRestartLevel", "OnLoadingLevel", "OnLevelLoaded", "OnMaplistList", "OnMaplistMapAppended", 
                "OnMaplistMapRemoved", "OnMaplistMapInserted", "OnMaplistNextLevelIndex", "OnMaplistGetMapIndices", "OnPlaylistSet");

            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;

            foreach (string gameMode in this.GetMapList("{GameMode}"))
            { // , "RUSH", "CONQUEST", "SQDM", "SQRUSH"
                //this.WritePluginConsole("Loading stage -> checking mode: " + gameMode);
                CMap anyMap = this.GetMapByFormattedName("{GameMode}", gameMode);
                //this.WritePluginConsole("Loading stage -> checking mode1: " + anyMap.PlayList);
                if (anyMap != null && anyMap.TeamNames.Count > 0)
                {
                    this.m_gameModeSettings.Add(new GamemodeInformation(gameMode, anyMap.PlayList, this.GetMapList("{FileName}", anyMap.PlayList).ToArray()));
                    // gameMode -> FriendlyName, PlayList -> ShortName like on server ... plugin CurGameMode -> ShortName
                    if (this.m_strSelectedGamemode.Length == 0)
                    {
                        this.m_strSelectedGamemode = gameMode;
                    }
                }
            }

        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdminInGameMapCommands: ^2Enabled!");
            this.WritePluginConsole(String.Format("Enabled for Gameserver: {0}", this.m_strGameServerType));
            this.m_isPluginEnabled = true;
            if (this.m_strGameServerType == "bf3") {
                this.ExecuteCommand("procon.protected.send", "mapList.list");
                this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices"); 
                this.ExecuteCommand("procon.protected.send", "serverInfo"); 
                this.ExecuteCommand("procon.protected.send", "admin.currentLevel"); 
            } else {
                this.ExecuteCommand("procon.protected.send", "mapList.list", "rounds");
                this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex"); 
                this.ExecuteCommand("procon.protected.send", "serverInfo"); 
                this.ExecuteCommand("procon.protected.send", "admin.currentLevel"); 
            }
            
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdminInGameMapCommands: ^1Disabled =(");
            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            if (this.m_strSelectedGamemode.Length > 0 && this.m_gameModeSettings.Contains(this.m_strSelectedGamemode) == true)
            {
                lstReturn.Add(new CPluginVariable(String.Format("Current Gamemode available maps|{0} - Allowed maplist", this.m_strSelectedGamemode), typeof(string[]), this.m_gameModeSettings[this.m_strSelectedGamemode].AllowedMapListFileNames));
                CMap theMap = this.GetMapByFormattedName("{GameMode}", this.m_strSelectedGamemode);
            }

            lstReturn.Add(new CPluginVariable("Commands|Map Command", this.m_strMapCommand.GetType(), this.m_strMapCommand));
            lstReturn.Add(new CPluginVariable("Commands|MapList Command", this.m_strMapListCommand.GetType(), this.m_strMapListCommand));
            lstReturn.Add(new CPluginVariable("Commands|Show next map Command", this.m_strShowNextMapCommand.GetType(), this.m_strShowNextMapCommand));
            lstReturn.Add(new CPluginVariable("Commands|Gamemode Command", this.m_strGameModeCommand.GetType(), this.m_strGameModeCommand));
            lstReturn.Add(new CPluginVariable("Commands|Confirm command", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            //lstReturn.Add(new CPluginVariable("Commands|Cancel command", this.m_strCancelCommand.GetType(), this.m_strCancelCommand));

            lstReturn.Add(new CPluginVariable("Map Stuff|Add map method", "enum.CAdminInGameMapCommands_AddMethod(append|insert)", this.m_strAddMapMethod));
            lstReturn.Add(new CPluginVariable("Map Stuff|Save maplist", typeof(enumBoolYesNo), this.m_enDoSaveNewMaplist));
            lstReturn.Add(new CPluginVariable("Map Stuff|Restart on zero", typeof(enumBoolYesNo), this.m_enDoMapRestartOnZero));
            lstReturn.Add(new CPluginVariable("Xtras|Console output", typeof(enumBoolYesNo), this.m_enDoConsoleOutput));
            lstReturn.Add(new CPluginVariable("Xtras|Debug output", typeof(enumBoolYesNo), this.m_enDoDebugOutput));
            //lstReturn.Add(new CPluginVariable("Xtras|Quiet mode", typeof(enumBoolYesNo), this.m_enDoQuietMode));
            lstReturn.Add(new CPluginVariable("Xtras|Enforce command registration", typeof(enumBoolYesNo), this.m_enDoEnforceComReg));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Map Command", this.m_strMapCommand.GetType(), this.m_strMapCommand));
            lstReturn.Add(new CPluginVariable("MapList Command", this.m_strMapListCommand.GetType(), this.m_strMapListCommand));
            lstReturn.Add(new CPluginVariable("Show next map Command", this.m_strShowNextMapCommand.GetType(), this.m_strShowNextMapCommand));
            lstReturn.Add(new CPluginVariable("Gamemode Command", this.m_strGameModeCommand.GetType(), this.m_strGameModeCommand));
            lstReturn.Add(new CPluginVariable("Confirm command", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            //lstReturn.Add(new CPluginVariable("Cancel command", this.m_strCancelCommand.GetType(), this.m_strCancelCommand));

            lstReturn.Add(new CPluginVariable("Add map method", "enum.CAdminInGameMapCommands_AddMethod(append|insert)", this.m_strAddMapMethod));
            lstReturn.Add(new CPluginVariable("Save maplist", typeof(enumBoolYesNo), this.m_enDoSaveNewMaplist));
            lstReturn.Add(new CPluginVariable("Restart on zero", typeof(enumBoolYesNo), this.m_enDoMapRestartOnZero));

            lstReturn.Add(new CPluginVariable("Console output", typeof(enumBoolYesNo), this.m_enDoConsoleOutput));
            lstReturn.Add(new CPluginVariable("Debug output", typeof(enumBoolYesNo), this.m_enDoDebugOutput));
            //lstReturn.Add(new CPluginVariable("Quiet mode", typeof(enumBoolYesNo), this.m_enDoQuietMode));
            lstReturn.Add(new CPluginVariable("Enforce command registration", typeof(enumBoolYesNo), this.m_enDoEnforceComReg));

            return lstReturn;
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {
            this.UnregisterAllCommands();

            if (strVariable.CompareTo("Map Command") == 0) {
                this.m_strMapCommand = strValue;
            }
            if (strVariable.CompareTo("MapList Command") == 0)
            {
                this.m_strMapListCommand = strValue;
            }
            if (strVariable.CompareTo("Show next map Command") == 0)
            {
                this.m_strShowNextMapCommand = strValue;
            }
            if (strVariable.CompareTo("Gamemode Command") == 0)
            {
                this.m_strGameModeCommand = strValue;
            }
            else if (strVariable.CompareTo("Confirm command") == 0)
            {
                this.m_strConfirmCommand = strValue;
            }
            else if (strVariable.CompareTo("Cancel command") == 0)
            {
                this.m_strCancelCommand = strValue;
            }
            else if (strVariable.CompareTo("Add map method") == 0)
            {
                this.m_strAddMapMethod = strValue;
            }
            else if (strVariable.CompareTo("Save maplist") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDoSaveNewMaplist = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Restart on zero") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDoMapRestartOnZero = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Console output") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDoConsoleOutput = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enDoConsoleOutput == enumBoolYesNo.No && this.m_enDoQuietMode == enumBoolYesNo.Yes && this.m_enDoDebugOutput == enumBoolYesNo.No)
                {
                    this.m_enDoConsoleOutput = enumBoolYesNo.Yes;
                }
            }
            else if (strVariable.CompareTo("Debug output") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDoDebugOutput = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enDoDebugOutput == enumBoolYesNo.No && this.m_enDoQuietMode == enumBoolYesNo.Yes)
                {
                    this.m_enDoConsoleOutput = enumBoolYesNo.Yes;
                }
            }
            else if (strVariable.CompareTo("Quiet mode") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDoQuietMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enDoQuietMode == enumBoolYesNo.Yes) this.m_enDoConsoleOutput = enumBoolYesNo.Yes;
            }
            else if (strVariable.CompareTo("Enforce command registration") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDoEnforceComReg = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            this.RegisterAllCommands();
        }

        private void UnregisterAllCommands()
        {
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("AdminInGameMapCommands: entering UnregisterAllCommands...");
            }
            List<string> emptyList = new List<string>();
            List<string> scopes = this.Listify<string>(this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix);

            // MapList.list all maps in gamemode
            this.UnregisterCommand(
                new MatchCommand(
                    "CAdminInGameMapCommands",
                    "OnCommandMapList_im",
                    scopes,
                    this.m_strMapCommand,
                    this.Listify<MatchArgumentFormat>(),
                    new ExecutionRequirements(ExecutionScope.All),
                    "Displays a list of available maps for the current gamemode."
                )
            );

            // MapList.set
            this.UnregisterCommand(
                new MatchCommand(
                    "CAdminInGameMapCommands",
                    "OnCommandMapSet_im",
                    scopes,
                    this.m_strMapCommand,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat("map name", emptyList),
                        new MatchArgumentFormat("optional: fast", emptyList)
                    ),
                    new ExecutionRequirements(ExecutionScope.All),
                    "Sets the next map to run.  Map can be a name or filename."
                )
            );

            // MapList.list the current maplist
            this.UnregisterCommand(
                new MatchCommand(
                    "CAdminInGameMapCommands",
                    "OnCommandMapListList_im",
                    scopes,
                    this.m_strMapListCommand,
                    this.Listify<MatchArgumentFormat>(),
                    new ExecutionRequirements(ExecutionScope.All),
                    "Displays the list of maps in the current MapList/Rotation."
                )
            );

            // ShowNextMap
            this.UnregisterCommand(
                new MatchCommand(
                    "CAdminInGameMapCommands",
                    "OnCommandShowNextMap_im",
                    scopes,
                    this.m_strShowNextMapCommand,
                    this.Listify<MatchArgumentFormat>(),
                    new ExecutionRequirements(ExecutionScope.All),
                    "Displays which map will be next."
                )
            );


            // GameMode.list
            this.UnregisterCommand(
                new MatchCommand(
                    "CAdminInGameMapCommands",
                    "OnCommandGameModeList_im",
                    scopes,
                    this.m_strGameModeCommand,
                    this.Listify<MatchArgumentFormat>(),
                    new ExecutionRequirements(ExecutionScope.All),
                    "Displays a list of available gamemodes."
                )
            );

            // GameMode.set
            this.UnregisterCommand(
                new MatchCommand(
                    "CAdminInGameMapCommands",
                    "OnCommandGameModeSet_im",
                    scopes,
                    this.m_strGameModeCommand,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat("PlayList name", emptyList),
                        new MatchArgumentFormat("optional: LevelFileName", emptyList),
                        new MatchArgumentFormat("optional: fast", emptyList),
                        new MatchArgumentFormat("optional: override", emptyList)
                    ),
                    new ExecutionRequirements(ExecutionScope.All),
                    "Displays a list of available maps for the current gamemode."
                )
            );
        }

        private void RegisterAllCommands()
        {
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("AdminInGameMapCommands: entering RegisterAllCommands...");
            }
            if (this.m_isPluginEnabled == true)
            {
                List<string> emptyList = new List<string>(); 
                List<string> scopes = this.Listify<string>(this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix);

                MatchCommand confirmationCommand_im = new MatchCommand(scopes, this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

                this.RegisterCommand(
                    new MatchCommand(
                        "CAdminInGameMapCommands",
                        "OnCommandMapList_im",
                        scopes,
                        this.m_strMapCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(ExecutionScope.All),
                        "Displays a list of available maps for the current gamemode."
                    )
                );

                List<string> tmp_CommandMapList = new List<string>();
                if (string.Equals(this.m_strCurGameMode, this.m_strNxtGameMode, StringComparison.OrdinalIgnoreCase) == false)
                {
                    tmp_CommandMapList = this.ListifyMaplist(m_gameModeSettings[this.m_strNxtPlayList].AllowedMapListFileNames, false);
                }
                else
                {
                    tmp_CommandMapList = this.ListifyMaplist(m_gameModeSettings[this.m_strSelectedGamemode].AllowedMapListFileNames, false);
                }

                this.RegisterCommand(
                    new MatchCommand(
                        "CAdminInGameMapCommands",
                        "OnCommandMapSet_im",
                        scopes,
                        this.m_strMapCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat("map name", tmp_CommandMapList),
                            new MatchArgumentFormat("optional: fast", emptyList)
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand_im,
                            "You do not have enough privileges to forward the map"
                        ),
                        "Sets the next map to run. Map can be a name or filename. Fast loads the map directly."
                    )
                );

                //mapList.list current Maplist
                this.RegisterCommand(
                    new MatchCommand(
                        "CAdminInGameMapCommands",
                        "OnCommandMapListList_im",
                        scopes,
                        this.m_strMapListCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(ExecutionScope.All),
                        "Displays the list of maps in the current MapList/Rotation."
                    )
                );

                // ShowNextMap
                this.RegisterCommand(
                    new MatchCommand(
                        "CAdminInGameMapCommands",
                        "OnCommandShowNextMap_im",
                        scopes,
                        this.m_strShowNextMapCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(ExecutionScope.All),
                        "Displays which map will be next."
                    )
                );

                // GameMode.list
                this.RegisterCommand(
                    new MatchCommand(
                        "CAdminInGameMapCommands",
                        "OnCommandGameModeList_im",
                        scopes,
                        this.m_strGameModeCommand,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(ExecutionScope.All),
                        "Displays a list of available gamemodes."
                    )
                );

                // GameMode.set 875-931 ... in MoH the list of playlist triggers but confirm command to set does recognized
                this.RegisterCommand(
                    new MatchCommand(
                        "CAdminInGameMapCommands",
                        "OnCommandGameModeSet_im",
                        scopes,
                        this.m_strGameModeCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat("PlayList name", ListifyPlaylist("{0}")),
                            new MatchArgumentFormat("optional: LevelFileName", emptyList),
                            new MatchArgumentFormat("optional: fast", emptyList),
                            new MatchArgumentFormat("optional: override", emptyList)
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.Privileges,
                            Privileges.CanUseMapFunctions,
                            0,
                            confirmationCommand_im,
                            "You do not have enough privileges to forward the map"
                        ),
                        "Sets the next PlayList/GameMode to run a map from. "
                    )
                );
            }
        }


        public void OnCommandMapList_im(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            List<string> mapList = new List<string>();
            if (string.Equals(this.m_strCurGameMode, this.m_strNxtGameMode, StringComparison.OrdinalIgnoreCase) == false)
            {
                mapList = this.ListifyMaplist(this.m_gameModeSettings[this.m_strNxtPlayList].AllowedMapListFileNames, false);
            }
            else
            {
                mapList = this.ListifyMaplist(this.m_gameModeSettings[this.m_strSelectedGamemode].AllowedMapListFileNames, false);
            }
            this.WriteMessage(String.Format("Available maps: {0}", String.Join(", ", mapList.ToArray())), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
        }

        public void OnCommandMapSet_im(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string tmp_fastforward = string.Empty;
            int tmp_iIsInIndex = -1;
 
            if (capCommand.ExtraArguments.Length > 0 && string.Equals("fast", capCommand.ExtraArguments, StringComparison.OrdinalIgnoreCase))
            {
                 tmp_fastforward = "fast";
            }

            CMap selectedMap = this.GetMapByFormattedName("{PublicLevelName} ({FileName})", capCommand.MatchedArguments[0].Argument);

            if (this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole(String.Format("Map selected: {0}", selectedMap.FileName));
                if (tmp_fastforward.Length > 0)
                {
                    this.WritePluginConsole("Map should be loaded directly.");
                }
            }

            this.WriteMessage(String.Format("Map selected: {0}", selectedMap.FileName), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
            for (int i = 0; i < this.m_lstCurMapList.Count; i++)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole(String.Format("{0} check list -> {1}", selectedMap.FileName, this.m_lstCurMapList[i].MapFileName));
                }
                if (string.Equals(this.m_lstCurMapList[i].MapFileName, selectedMap.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                    {
                        this.WritePluginConsole(String.Format("Map {0} found in curMapList with index {1}.", selectedMap.FileName, i));
                    }
                    tmp_iIsInIndex = i;
                    break;
                }
            }
            // selected map is in rotation and the next map
            if (tmp_iIsInIndex == this.m_iCurMapIndex + 1 || (tmp_iIsInIndex == 0 && this.m_iCurMapIndex == this.m_lstCurMapList.Count - 1))
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Next map in normal rotation was selected.");
                }
                this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex", tmp_iIsInIndex.ToString());
                if (tmp_fastforward.Length > 0)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextRound");
                }
                else
                {
                    this.WriteMessage(String.Format("Selected map ({0}) is the next map. Use InGameAdmin 'nextlevel' command to forward.", selectedMap.FileName), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }
            else if (tmp_iIsInIndex == this.m_iCurMapIndex)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Running map selected.");
                }
                this.WriteMessage(String.Format("You selected the running map. Use InGameAdmin 'restart' command to forward.", selectedMap.FileName), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
            }
            else if (tmp_iIsInIndex > -1)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Selected map is in rotation. Setting nextLevelIndex.");
                }
                this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex", tmp_iIsInIndex.ToString());
                if (tmp_fastforward.Length > 0)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextRound");
                }
                else
                {
                    this.WriteMessage(String.Format("Next map is {0}. Use InGameAdmin 'nextlevel' command to forward.", selectedMap.FileName), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }
            else
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Selected map is NOT in rotation. Appending/insterting map and setting nextLevelIndex.");
                }
                int tmp_insertIndex = this.m_iCurMapIndex + 1;
                if (string.Compare("append", this.m_strAddMapMethod, true) == 0) { tmp_insertIndex = this.m_lstCurMapList.Count; }
                this.ExecuteCommand("procon.protected.send", "mapList.insert", tmp_insertIndex.ToString(), selectedMap.FileName, "2");
                this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex", tmp_insertIndex.ToString());
                if (this.m_enDoSaveNewMaplist == enumBoolYesNo.Yes)
                {
                    if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                    {
                        this.WritePluginConsole("Changed rotation saved.");
                    }
                    this.ExecuteCommand("procon.protected.send", "mapList.save");
                }
                // doubled with insert-event: this.ExecuteCommand("procon.protected.send", "mapList.list");
                if (tmp_fastforward.Length > 0)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextRound");
                }
                else
                {
                    this.WriteMessage(String.Format("Next map is {0}. Use InGameAdmin 'nextlevel' command to forward.", selectedMap.FileName), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }
        }

        //this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", selectedMap.PlayList);
        //this.ExecuteCommand("procon.protected.send", "mapList.clear");
        //this.ExecuteCommand("procon.protected.send", "mapList.append", selectedMap.FileName);
        //this.ExecuteCommand("procon.protected.send", "admin.runNextLevel");

        public void OnCommandMapListList_im(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("Current mapList was requested.");
            }
            this.WriteMessage("Maps in the current mapList/Rotation:", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
            for (int i = 0; i < this.m_lstCurMapList.Count; i++)
            {
                CMap tmp_Map = this.GetMapByFilename(this.m_lstCurMapList[i].MapFileName);
                this.WriteMessage(String.Format("{0} - {1} ({2})", i.ToString(), tmp_Map.PublicLevelName, this.m_lstCurMapList[i].MapFileName), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
            }
          }

        public void OnCommandShowNextMap_im(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("Info which map is next was requested.");
            }
            // this.m_iNextMapIndex / this.m_iNextMapIndexOld ; only different if next map is not a new round on same map

            if ((this.m_blFinalRoundSet == false || this.m_iNextMapIndex == this.m_iNextMapIndexOld) && this.m_iNextMapIndexOld != -1)
            {
                CMap tmp_Map = this.GetMapByFilename(this.m_lstCurMapList[this.m_iCurMapIndex].MapFileName);
                int tmp_MaxRounds = (this.m_lstCurMapList[this.m_iCurMapIndex].Rounds == 0) ? 2 : this.m_lstCurMapList[this.m_iCurMapIndex].Rounds;
                if (string.Equals(this.m_strCurGameMode, this.m_strNxtGameMode, StringComparison.OrdinalIgnoreCase) == false)
                {
                    this.WriteMessage(String.Format("Next GameMode: {0}", this.m_strNxtGameMode), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                    this.WriteMessage(String.Format("Next round will be on: {0} ({1}) Rounds: {2}", tmp_Map.PublicLevelName, this.m_lstCurMapList[this.m_iCurMapIndex].MapFileName, tmp_MaxRounds.ToString()), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
                else
                {
                    this.WriteMessage(String.Format("Next round will be on: {0} ({1}) Rounds: {2}/{3}", tmp_Map.PublicLevelName, this.m_lstCurMapList[this.m_iCurMapIndex].MapFileName, this.m_iCurMapRounds, tmp_MaxRounds.ToString()), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }

            if (this.m_iNextMapIndex != this.m_iNextMapIndexOld)
            {
                if (this.m_iNextMapIndex != -1)
                {
                    CMap tmp_Map = this.GetMapByFilename(this.m_lstCurMapList[this.m_iNextMapIndex].MapFileName);
                    int tmp_MaxRounds = (this.m_lstCurMapList[this.m_iNextMapIndex].Rounds == 0) ? 2 : this.m_lstCurMapList[this.m_iNextMapIndex].Rounds;
                    this.WriteMessage(String.Format("Next map will be: {0} ({1}) Rounds: {2}", tmp_Map.PublicLevelName, this.m_lstCurMapList[this.m_iNextMapIndex].MapFileName, tmp_MaxRounds.ToString()), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                    if (string.Equals(this.m_strCurGameMode, this.m_strNxtGameMode, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        this.WriteMessage(String.Format("Next GameMode: {0}", this.m_strNxtGameMode), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                    }
                }
                else
                {
                    this.WriteMessage("Next map will be: Random.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                    this.WriteMessage("Possible Reason: GameMode was changed without setting a new map.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                    this.WriteMessage(String.Format("Next GameMode: {0}", this.m_strNxtGameMode), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }
        }

        // ==> GameMode Section <==
        public void OnCommandGameModeList_im(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            // conquest/cq => 32 player, rush/gr => 32 player, sqrush/sr -> 8 player, sqdm/dm -> 16 player
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("^bThe List of GameModes was requested.");
            }

            List<string> PlaylistList = new List<string>();
            PlaylistList = ListifyPlaylist("{0} - {1} ({2} maxPlayer)");
            this.WriteMessage(String.Format("Available GameModes/PlayLists: {0}", String.Join(", ", PlaylistList.ToArray())), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
        }

        public void OnCommandGameModeSet_im(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string tmp_strNewPlayList = capCommand.MatchedArguments[0].Argument.ToUpper(); // short
            string tmp_strNewGameMode = this.GetTeamList("{GameMode}", tmp_strNewPlayList)[0]; // friendly

            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole(String.Format("^bA PlayList/GameMode change was issued => {0} ({1})", tmp_strNewPlayList, tmp_strNewGameMode));
            }

            bool NextModeOK = false;
            bool Override = false;
            bool fastForward = false;
            bool loadAllMaps = false;
            string tmp_LevelName = string.Empty;
            string tmp_newMap = string.Empty;
            bool tmp_newMapOK = false;
            int tmp_maxPlayers = 32;
            string[] tmp_keywords;
            
            if (capCommand.ExtraArguments.Length > 0 )
            {
                tmp_keywords = capCommand.ExtraArguments.Split(' ');
                foreach (string s in tmp_keywords)
                {
                    if (s.Equals("fast")) {
                        fastForward = true;
                    }
                    if (s.Equals("all")) {
                        loadAllMaps = true;
                    }
                    if (s.ToUpper().Contains("LEVELS"))
                    {
                        tmp_newMap = s; // Need check if map is in allowedMapNames!!!
                    }
                    if (s.Equals("override")) {
                        Override = true;
                        NextModeOK = true;
                    }
                }
                
                if (loadAllMaps == false && tmp_newMap != string.Empty)
                {
                    foreach (string allowedMap in this.m_gameModeSettings[tmp_strNewGameMode].AllowedMapListFileNames)
                    {
                        if (String.Equals(allowedMap, tmp_newMap, StringComparison.OrdinalIgnoreCase))
                        {
                            tmp_newMap = allowedMap;
                            tmp_newMapOK = true;
                        }
                    }
                }
                if (tmp_newMapOK == false) { tmp_newMap = string.Empty; }
            }
            
            // all vars are set/checked now continue...

            // need to check player count against maxPlayer in PlayList/GameMode .. idea... 'override'
            // conquest/cq => 32 player, rush/gr => 32 player, sqrush/sr -> 8 player, sqdm/dm -> 16 player / this.m_csiLatestServerInfo.PlayerCount
            if (NextModeOK == false && Override == false)
            {
                if (tmp_strNewPlayList.Equals("SQRUSH") && this.m_csiLatestServerInfo.PlayerCount <= 8)
                {
                    NextModeOK = true;
                    tmp_maxPlayers = 8;
                }
                if (tmp_strNewPlayList.Equals("SQDM") && this.m_csiLatestServerInfo.PlayerCount <= 16)
                {
                    NextModeOK = true;
                    tmp_maxPlayers = 16;
                }
                if ((tmp_strNewPlayList.Equals("RUSH") || tmp_strNewPlayList.Equals("CONQUEST")) && this.m_csiLatestServerInfo.PlayerCount <= 32)
                {
                    NextModeOK = true;
                    tmp_maxPlayers = 32;
                }
            }

            if (NextModeOK == false)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole(String.Format("^bA PlayList/GameMode {0} ({1}) only allows {2} maxPlayers. Order to override was NOT given.", tmp_strNewPlayList, tmp_strNewGameMode, tmp_maxPlayers.ToString()));
                }
                this.WriteMessage("You selected a PlayList/GameMode for less players than actual playing.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                this.WriteMessage("If you want to boot random player use the 'override' keyword.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
            }
            if (Override == true)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole(String.Format("^bOverride-Order to change PlayListGameMode to {0} ({1}) regardless of playerCount was given.", tmp_strNewPlayList, tmp_strNewGameMode, tmp_maxPlayers.ToString()));
                }
                this.WriteMessage("PlayList/GameMode will be changed regardless to playerCount. Random players might be kicked.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
            }

            // gamemode ok, all maps, fast forward ?
            if (NextModeOK && loadAllMaps)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole(string.Format("Adding of all maps for {0} was issued. FastForward: {1}.", tmp_strNewGameMode, fastForward.ToString()));
                }

                this.WriteMessage(String.Format("Next GameMode: {0}. All available maps will be added to rotation.", tmp_strNewGameMode), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);

                List<string> tmp_newGameModeMapList = new List<string>();
                tmp_newGameModeMapList = this.ListifyMaplist(m_gameModeSettings[tmp_strNewGameMode].AllowedMapListFileNames, true);

                this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", tmp_strNewPlayList);
                this.ExecuteCommand("procon.protected.send", "mapList.clear");
                foreach (string newMapFile in tmp_newGameModeMapList)
                {
                    this.ExecuteCommand("procon.protected.send", "mapList.append", newMapFile);
                }
                if (fastForward)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextLevel");
                }
                else
                {
                    this.WriteMessage(String.Format("Next map is {0}. Use InGameAdmin 'nextlevel' command to forward.", tmp_newGameModeMapList[0]), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }

            // gamemode ok, one map, fast forward ?
            if (NextModeOK && loadAllMaps == false && tmp_newMapOK)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole(string.Format("Map {0} for {1} added. FastForward: {2}.", tmp_newMap, tmp_strNewGameMode, fastForward.ToString()));
                }

                this.WriteMessage(String.Format("Next GameMode: {0}. Added map {1} to rotation.", tmp_strNewGameMode, tmp_newMap), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", tmp_strNewPlayList);
                this.ExecuteCommand("procon.protected.send", "mapList.clear");
                this.ExecuteCommand("procon.protected.send", "mapList.append", tmp_newMap);
                if (fastForward)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextLevel");
                }
                else
                {
                    this.WriteMessage("Use InGameAdmin 'nextlevel' command to forward.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }

            // gamemode ok, no map, fast forward ?
            if (NextModeOK && loadAllMaps == false && tmp_newMapOK == false)
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole(string.Format("GameMode {0} without setting a map. FastForward: {1}", tmp_strNewGameMode, fastForward.ToString()));
                }
                this.WriteMessage(String.Format("Next GameMode: {0}. NO map added to rotation.", tmp_strNewGameMode), new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                this.WriteMessage("Next map will be RANDOM. Set one with the map selection command!", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", tmp_strNewPlayList);
                this.ExecuteCommand("procon.protected.send", "mapList.clear");
                
                int irand = 0;
                Random tmp_rand = new Random();
                List<string> tmp_newGameModeMapList = new List<string>();
                tmp_newGameModeMapList = this.ListifyMaplist(m_gameModeSettings[tmp_strNewGameMode].AllowedMapListFileNames, true);
                
                if (this.m_strGameServerType == "bc2") { irand = 8; }
                if (this.m_strGameServerType == "moh") { irand = 2; }
                int tmp_iRandom = tmp_rand.Next(irand);
                
                this.ExecuteCommand("procon.protected.send", "mapList.append", tmp_newGameModeMapList[tmp_iRandom]);

                if (fastForward)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.runNextLevel");
                }
                else
                {
                    this.WriteMessage("Use InGameAdmin 'nextlevel' command to forward.", new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker), capCommand.ResposeScope);
                }
            }

            // all done check if mapList should be saved.
            if (this.m_enDoSaveNewMaplist == enumBoolYesNo.Yes && NextModeOK && (tmp_newMapOK || loadAllMaps))
            {
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Changed rotation saved.");
                }
                this.ExecuteCommand("procon.protected.send", "mapList.save");
            }
        }

#region events

        public virtual void OnCurrentLevel(string mapFileName) 
        {
            this.m_strCurMapFileName = mapFileName;
            CMap tmp_MapData = this.GetMapByFilename(this.m_strCurMapFileName);
            this.m_strCurGameMode = tmp_MapData.PlayList; // ShortName
            if (this.m_strNxtGameMode == string.Empty)
            {
                this.m_strNxtGameMode = this.m_strCurGameMode;
                this.m_strNxtPlayList = this.GetTeamList("{GameMode}", this.m_strNxtGameMode)[0];
            }
            string tmp_GameMode = tmp_MapData.GameMode; // FriendlyName
            this.m_strSelectedGamemode = tmp_GameMode;
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("^bCurrentLevel was fired!");
                this.WritePluginConsole("Current GameMode is: " + this.m_strCurGameMode + " -- " + this.m_strSelectedGamemode);
                this.WritePluginConsole("Current map is: " + this.m_strCurMapFileName);
            }
            this.CheckCurMapIndex();
            if (this.m_enDoEnforceComReg == enumBoolYesNo.Yes)
            {
                this.UnregisterAllCommands();
                this.RegisterAllCommands();
            }
            else
            {
                this.RegisterAllCommands();
            }
        }

        public override void OnServerInfo(CServerInfo serverInfo)
        {
            this.m_csiLatestServerInfo = serverInfo;

            switch (this.m_csiLatestServerInfo.GameMod.ToString())
            {
                case "BC2":
                    //
                    this.m_strGameType = "bc2";
                    break;
                case "VIETNAM":
                    this.m_strGameType = "vietnam";
                    break;
                default:
                    if (this.m_strGameServerType == "bf3") {
                        this.m_strGameType = "bf3";
                    }
                    // (R9)-MoH has no GameMod in serverInfo
                    if (this.m_strGameServerType == "moh") {
                        this.m_strGameType = "moh";
                    }
                    break;
            }

            this.m_strCurMapFileName = serverInfo.Map;
            this.m_strCurGameMode = serverInfo.GameMode; // ShortName
            if (this.m_strNxtGameMode == string.Empty)
            {
                this.m_strNxtGameMode = this.m_strCurGameMode;
                this.m_strNxtPlayList = this.GetTeamList("{GameMode}", this.m_strNxtGameMode)[0];
            }
            CMap tmp_MapData = this.GetMapByFilenamePlayList(this.m_strCurMapFileName, this.m_strCurGameMode);
            string tmp_GameMode = tmp_MapData.GameMode; // FriendlyName
            this.m_strSelectedGamemode = tmp_GameMode;
            this.m_iCurMapRounds = serverInfo.CurrentRound;
             if (this.m_strGameServerType == "bf3") { this.m_iCurMapRounds = this.m_iCurMapRounds + 1; }
            this.m_iCurMapMaxRounds = serverInfo.TotalRounds;

            this.m_blNewRoundStarted = false;
            if (serverInfo.RoundTime < 23)
            {
                this.m_blNewRoundStarted = true;
            }
            if (this.m_iCurMapRounds == this.m_iCurMapMaxRounds)
            {
                this.m_blFinalRoundSet = true;
            }
            else
            {
                this.m_blFinalRoundSet = false;
            }

            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("^bServerInfo was fired!");
                this.WritePluginConsole("GameType is: " + this.m_strGameType);
                this.WritePluginConsole("Current GameMode is: " + this.m_strCurGameMode + " -- " + this.m_strSelectedGamemode);
                this.WritePluginConsole("Current map is: " + this.m_strCurMapFileName);
                this.WritePluginConsole("New map just started?: " + (this.m_blNewRoundStarted ? "true" : "false") );
            }
            if (this.m_strGameServerType == "bf3") {
                this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            } else {
                this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex");
            }
            this.CheckCurMapIndex();

            if (this.m_enDoEnforceComReg == enumBoolYesNo.Yes)
            {
                this.UnregisterAllCommands();
            }
            
            this.RegisterAllCommands();
            if (this.m_enDoMapRestartOnZero == enumBoolYesNo.Yes)
            {
                this.checkDoMapRestartOnZero();
            }
        }

        public override void OnMaplistList(List<MaplistEntry> MapList)
        {
            this.m_lstCurMapList = MapList;
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("MapList.list was fired!");
                this.WritePluginConsole("MapList.list contains " + this.m_lstCurMapList.Count + " maps.");
                for (int i = 0; i < this.m_lstCurMapList.Count; i++)
                {
                    if (this.m_strGameServerType == "bf3") {
                        this.WritePluginConsole(i + " - " + this.m_lstCurMapList[i].Gamemode + " - " + this.m_lstCurMapList[i].MapFileName + " - " + this.m_lstCurMapList[i].Rounds + " - " + this.m_lstCurMapList[i].Index);
                    } else {
                        this.WritePluginConsole(i + " - " + this.m_lstCurMapList[i].MapFileName + " - " + this.m_lstCurMapList[i].Rounds);
                    }
                }
            }
            this.CheckCurMapIndex();
            this.RegisterAllCommands();
        }

        public virtual void OnMaplistNextLevelIndex(int mapIndex) 
        {
            this.m_blNextMapNew = false;
            if (this.m_iNextMapIndexOld != this.m_iNextMapIndex && m_blNewRoundStarted == true && this.m_blFinalRoundSet == false)
            {
                this.m_iNextMapIndexOld = this.m_iNextMapIndex;
                this.m_blNextMapNew = true; // mostly this hits in the current map, name points to command not "next level"
            }
            this.m_iNextMapIndex = mapIndex;

            // check next PlayList/GameMode for MoH on NextLevelIndex
            if ((this.m_strGameType == "moh" || this.m_strGameServerType == "moh") && this.m_blFinalRoundSet == true && this.m_lstCurMapList.Count > 0)
            {
                CMap tmp_Map = this.GetMapByFilename(this.m_lstCurMapList[mapIndex].MapFileName);
                this.m_strNxtGameMode = tmp_Map.PlayList; // ShortName
                this.m_strNxtPlayList = tmp_Map.GameMode; // FriendlyName
            }

            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole(String.Format("NextLevelIndex: new {0} - old {1}.", this.m_iNextMapIndex, this.m_iNextMapIndexOld));
                if (this.m_strGameType == "moh")
                {
                    this.WritePluginConsole(String.Format("NextLevelIndex PlayList/GameMode: {0} / {1}.", this.m_strNxtGameMode, this.m_strNxtPlayList));
                }
            }
        }

        public virtual void OnMaplistGetMapIndices(int mapIndex, int nextIndex) { 
            this.m_blNextMapNew = false;
            if (this.m_iNextMapIndexOld != this.m_iNextMapIndex && m_blNewRoundStarted == true && this.m_blFinalRoundSet == false)
            {
                this.m_iNextMapIndexOld = this.m_iNextMapIndex;
                this.m_blNextMapNew = true; // mostly this hits in the current map, name points to command not "next level"
            }
            this.m_iNextMapIndex = nextIndex;

            // check next PlayList/GameMode for BF3 on NextLevelIndex
            if ((this.m_strGameType == "bf3h" || this.m_strGameServerType == "bf3") && this.m_blFinalRoundSet == true && this.m_lstCurMapList.Count > 0)
            {
                CMap tmp_Map = this.GetMapByFilename(this.m_lstCurMapList[mapIndex].MapFileName);
                this.m_strNxtGameMode = tmp_Map.PlayList; // ShortName
                this.m_strNxtPlayList = tmp_Map.GameMode; // FriendlyName
            }

            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole(String.Format("GetMapIndices: current {2} - new {0} - old {1}.", this.m_iNextMapIndex, this.m_iNextMapIndexOld, mapIndex));
                if (this.m_strGameType == "bf3")
                {
                    this.WritePluginConsole(String.Format("GetMapIndices PlayList/GameMode: {0} / {1}.", this.m_strNxtGameMode, this.m_strNxtPlayList));
                }
            }
        }

        #region event_helper
        //"OnMaplistLoad", "OnMaplistSave", "OnMaplistMapAppended", "OnMaplistMapRemoved", "OnMaplistMapInserted"
        public virtual void OnMaplistLoad() { this.ExecuteCommand("procon.protected.send", "mapList.list", "rounds"); }
        //public virtual void OnMaplistSave() { this.ExecuteCommand("procon.protected.send", "mapList.list", "rounds"); }
        public virtual void OnMaplistMapAppended(string mapFileName) { this.ExecuteCommand("procon.protected.send", "mapList.list", "rounds"); }
        public virtual void OnMaplistMapRemoved(int mapIndex) { this.ExecuteCommand("procon.protected.send", "mapList.list", "rounds"); }
        public virtual void OnMaplistMapInserted(int mapIndex, string mapFileName) { this.ExecuteCommand("procon.protected.send", "mapList.list", "rounds"); }
        // "OnRestartLevel", "OnLoadingLevel", "OnLevelLoaded"
        public virtual void OnRestartLevel() { 
            this.ExecuteCommand("procon.protected.send", "admin.currentLevel"); 
            this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex"); 
        }
        public virtual void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) 
        {
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("^bOnLoadingLevel was fired! -> " + mapFileName);
            }
            this.m_strCurMapFileName = mapFileName;
            this.m_iCurMapRounds = roundsPlayed;
            this.m_iCurMapMaxRounds = roundsTotal;
            this.ExecuteCommand("procon.protected.send", "mapList.nextLevelIndex");
            this.CheckCurMapIndex();
            this.RegisterAllCommands();
        }
        public virtual void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {
            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("^bOnLevelLoaded was fired! -> " + mapFileName);
            }
            this.m_blNewRoundStarted = true;
            this.m_strCurMapFileName = mapFileName;
            this.m_iCurMapRounds = roundsPlayed+1;
            this.m_iCurMapMaxRounds = roundsTotal;
            this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            this.CheckCurMapIndex();
            this.RegisterAllCommands();
        }
        // "OnPlaylistSet
        public virtual void OnPlaylistSet(string playlist) 
        {
            //this.m_strCurGameMode = playlist; // ShortName
            //this.m_strSelectedGamemode = this.m_strCurGameMode; 
            
            this.m_strNxtGameMode = playlist.ToUpper(); // ShortName
            this.m_strNxtPlayList = this.GetTeamList("{GameMode}", this.m_strNxtGameMode)[0]; // FriendlyName

            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
            {
                this.WritePluginConsole("^bOnPlaylistSet was fired! -> " + playlist); 
                this.WritePluginConsole("Current GameMode is: " + this.m_strCurGameMode + " -- " + this.m_strSelectedGamemode);
                this.WritePluginConsole("Next Gamemode is set: " + this.m_strNxtGameMode + " -- " + this.m_strNxtPlayList);
            }
            //this.UnregisterAllCommands();
            this.RegisterAllCommands();
        }
        #endregion event_helper

#endregion events

#region helper_functions

        private void WritePluginConsole(string message)
        {
            string line = String.Format("AdminInGameMapCommands: {0}", message);
            this.ExecuteCommand("procon.protected.pluginconsole.write", line);
        }

        private void WriteMessage(string message, CPlayerSubset audience, string strScope)
        {
            List<string> wordWrappedLines = this.WordWrap(message, 100);
            foreach (string line in wordWrappedLines)
            {
                string formattedLine = String.Format("{0}", line);
                if (audience.Subset == CPlayerSubset.PlayerSubsetType.All)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "all");
                }
                else if (audience.Subset == CPlayerSubset.PlayerSubsetType.Player)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "player", audience.SoldierName);
                }
            }
        }

        private void CheckCurMapIndex()
        {
            if ((this.m_strCurMapFileName != "" && this.m_lstCurMapList.Count > 0) && this.m_iNextMapIndexOld == -1)
            {
                for (int i = 0; i < this.m_lstCurMapList.Count; i++)
                {
                    if (string.Equals(this.m_lstCurMapList[i].MapFileName, this.m_strCurMapFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        this.m_iCurMapIndex = i;
                        if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                        {
                            this.WritePluginConsole("A-Current map index is " + this.m_iCurMapIndex);
                        }
                        break;
                    }
                }
            }

            // NEED BETTER CHECK AS Above ... rounds nextIndex m_iNextMapIndex m_iNextMapIndexOld
            if (this.m_iNextMapIndex != -1 && this.m_iNextMapIndex != this.m_iNextMapIndexOld && this.m_iNextMapIndexOld > -1)
            {
                if (this.m_iNextMapIndexOld != -1)
                {
                    if (this.m_blNextMapNew == true) // && this.m_iCurMapRounds == this.m_iCurMapMaxRounds)
                    {
                        this.m_iCurMapIndex = this.m_iNextMapIndexOld;
                        if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                        {
                            this.WritePluginConsole("B-1-Current map index is " + this.m_iCurMapIndex);
                        }
                    }
                    else
                    {
                        if (this.m_blFinalRoundSet == false && this.m_blNewRoundStarted == true)
                        {
                            this.m_iCurMapIndex = m_iNextMapIndex;
                            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                            {
                                this.WritePluginConsole("B-2-Current map index is " + this.m_iCurMapIndex);
                            }
                        }
                    }
                }
            }
            else if (this.m_iNextMapIndexOld != -1 && this.m_blFinalRoundSet == false && this.m_blNewRoundStarted == true)
            {
                this.m_iCurMapIndex = this.m_iNextMapIndexOld;
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("C-Current map index is " + this.m_iCurMapIndex);
                }
            }
        }

        private void checkDoMapRestartOnZero()
        {
            // m_enDoMapRestartOnZero , m_blReadyToRestart , m_csiLatestServerInfo

            if (this.m_csiLatestServerInfo.PlayerCount >= 4 && this.m_blReadyToRestart == false) {
                this.m_blReadyToRestart = true;
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Server populated again with PlayerCount >= 4. InternalRestartOnZero set active.");
                }
            }

            if (this.m_blReadyToRestart == true && this.m_csiLatestServerInfo.PlayerCount == 0)
            {
                this.m_blReadyToRestart = false;
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes || this.m_enDoConsoleOutput == enumBoolYesNo.Yes)
                {
                    this.WritePluginConsole("Round is being restarted. PlayerCount hit zero.");
                }
                this.ExecuteCommand("procon.protected.send", "admin.restartRound");
            }
        }

        private List<string> ListifyMaplist(string[] mapList, bool onlyFileName)
        {
            List<string> returnMapList = new List<string>();
            List<CMap> mapDefines = this.GetMapDefines();
            foreach (string allowedMap in mapList)
            {
                foreach (CMap map in mapDefines)
                {
                    if (String.Compare(map.FileName, allowedMap, true) == 0)
                    {
                        if (onlyFileName == false)
                        {
                            returnMapList.Add(String.Format("{0} ({1})", map.PublicLevelName, map.FileName));
                        }
                        else
                        {
                            returnMapList.Add(String.Format("{0}", map.FileName));
                        }
                    }
                }
            }

            return returnMapList;
        }

        private List<string> ListifyPlaylist(string myFormat)
        {
            // conquest/cq => 32 player, rush/gr => 32 player, sqrush/sr -> 8 player, sqdm/dm -> 16 player

            List<string> returnGameModeList = new List<string>();
            int i = 1;
            foreach (GamemodeInformation PlayListInfo in this.m_gameModeSettings) {
                string PlayList = PlayListInfo.PlayListName;
                int maxPlayer = 32;
                if (PlayList.Equals("SQDM")) { maxPlayer = 16; }
                if (PlayList.Equals("SQRUSH")) { maxPlayer = 8; }
                //returnGameModeList.Add(String.Format("{0} - {1} - ({2})", PlayListInfo.PlayListName, PlayListInfo.Name, maxPlayer.ToString()));
                returnGameModeList.Add(String.Format(myFormat, PlayListInfo.PlayListName, PlayListInfo.Name, maxPlayer.ToString()));
                if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                {
                    //this.WritePluginConsole(String.Format("{2} - {1} ({0})", PlayListInfo.PlayListName, PlayListInfo.Name, maxPlayer.ToString()));
                }
                i++;
            }
            return returnGameModeList;
        }

#endregion

#region helper_classes

        internal class GamemodeInformationDictionary : KeyedCollection<string, GamemodeInformation>
        {

            protected override string GetKeyForItem(GamemodeInformation item)
            {
                return item.Name;
            }

            public new GamemodeInformation this[string key]
            {
                get
                {
                    GamemodeInformation keyedGamemode = null;

                    foreach (GamemodeInformation gamemode in this)
                    {
                        if (String.Compare(gamemode.Name, key, true) == 0)
                        {
                            keyedGamemode = gamemode;
                            break;
                        }
                    }

                    return keyedGamemode;
                }
            }

            public new bool Contains(string key)
            {

                bool isKeyed = false;

                foreach (GamemodeInformation gamemode in this)
                {
                    if (String.Compare(gamemode.Name, key, true) == 0)
                    {
                        isKeyed = true;
                        break;
                    }
                }

                return isKeyed;
            }
        }

        internal class GamemodeInformation
        {

            private string m_gamemodeName;
            public string Name
            {
                get
                {
                    return this.m_gamemodeName;
                }
            }

            private string m_playListName;
            public string PlayListName
            {
                get
                {
                    return this.m_playListName;
                }
            }

            private string[] ma_allowedMapListFileNames;
            public string[] AllowedMapListFileNames
            {
                get
                {
                    return this.ma_allowedMapListFileNames;
                }
                set
                {
                    this.ma_allowedMapListFileNames = value;
                }
            }

            public GamemodeInformation(string gameMode, string playlistName, string[] a_allowedMapListFileNames)
            {
                this.m_gamemodeName = gameMode;
                this.m_playListName = playlistName;

                this.AllowedMapListFileNames = a_allowedMapListFileNames;
            }
        }

#endregion

    }
}