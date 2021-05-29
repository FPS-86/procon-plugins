using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CSettingChangeOnMap : PRoConPluginAPI, IPRoConPluginInterface
    {
        private int m_mapIndex;
        private int m_nextIndex;
        private List<string> myMapList;
        private List<string> gameMode;
        private List<int> numRounds;
        private List<string> variableNames;
        private List<List<string>> customVariables;
        private List<string> defaultVariables;
        private List<string> defaultVariablesBackup;

        private bool cameFromRunNext;
        private bool needsMapInitialization;
        private bool needsModeChange;
        private List<bool> variableHasBeenSet;
        private enumBoolYesNo refreshMapList;

        private void setNormalMode()
        {
            // Normal Preset Variables
            this.ExecuteCommand("procon.protected.send", "vars.autoBalance", "true");
            this.ExecuteCommand("procon.protected.send", "vars.friendlyFire", "false");
            this.ExecuteCommand("procon.protected.send", "vars.killCam", "true");
            this.ExecuteCommand("procon.protected.send", "vars.miniMap", "true");
            this.ExecuteCommand("procon.protected.send", "vars.hud", "true");
            this.ExecuteCommand("procon.protected.send", "vars.3dSpotting", "true");
            this.ExecuteCommand("procon.protected.send", "vars.nameTag", "true");
            this.ExecuteCommand("procon.protected.send", "vars.3pCam", "true");
            this.ExecuteCommand("procon.protected.send", "vars.regenerateHealth", "true");
            this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
            this.ExecuteCommand("procon.protected.send", "vars.soldierHealth", "100");
            this.ExecuteCommand("procon.protected.send", "vars.playerRespawnTime", "100");
            this.ExecuteCommand("procon.protected.send", "vars.onlySquadLeaderSpawn", "false");

            // Remaining Variables Set From User's Default Variable List
            for (int i = 13; i < defaultVariables.Count; i++)
            {
                if (defaultVariables[i] != "YOUR_SERVER_NAME_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_MAX_PLAYERS_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_SERVER PASSWORD_HERE (Skip to Ignore)")
                    this.ExecuteCommand("procon.protected.send", variableNames[i], defaultVariables[i]);
            }
        }

        private void setInfantryMode()
        {
            // Infantry Only Preset Variables
            this.ExecuteCommand("procon.protected.send", "vars.autoBalance", "true");
            this.ExecuteCommand("procon.protected.send", "vars.friendlyFire", "false");
            this.ExecuteCommand("procon.protected.send", "vars.killCam", "true");
            this.ExecuteCommand("procon.protected.send", "vars.miniMap", "true");
            this.ExecuteCommand("procon.protected.send", "vars.hud", "true");
            this.ExecuteCommand("procon.protected.send", "vars.3dSpotting", "true");
            this.ExecuteCommand("procon.protected.send", "vars.nameTag", "true");
            this.ExecuteCommand("procon.protected.send", "vars.3pCam", "false");
            this.ExecuteCommand("procon.protected.send", "vars.regenerateHealth", "true");
            this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "false");
            this.ExecuteCommand("procon.protected.send", "vars.soldierHealth", "100");
            this.ExecuteCommand("procon.protected.send", "vars.playerRespawnTime", "100");
            this.ExecuteCommand("procon.protected.send", "vars.onlySquadLeaderSpawn", "false");

            // Remaining Variables Set From User's Default Variable List
            for (int i = 13; i < defaultVariables.Count; i++)
            {
                if (defaultVariables[i] != "YOUR_SERVER_NAME_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_MAX_PLAYERS_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_SERVER PASSWORD_HERE (Skip to Ignore)")
                    this.ExecuteCommand("procon.protected.send", variableNames[i], defaultVariables[i]);
            }
        }

        private void setHardcoreMode()
        {
            // Hardcore Preset Variables
            this.ExecuteCommand("procon.protected.send", "vars.autoBalance", "true");
            this.ExecuteCommand("procon.protected.send", "vars.friendlyFire", "true");
            this.ExecuteCommand("procon.protected.send", "vars.killCam", "false");
            this.ExecuteCommand("procon.protected.send", "vars.miniMap", "true");
            this.ExecuteCommand("procon.protected.send", "vars.hud", "false");
            this.ExecuteCommand("procon.protected.send", "vars.3dSpotting", "false");
            this.ExecuteCommand("procon.protected.send", "vars.nameTag", "false");
            this.ExecuteCommand("procon.protected.send", "vars.3pCam", "false");
            this.ExecuteCommand("procon.protected.send", "vars.regenerateHealth", "false");
            this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
            this.ExecuteCommand("procon.protected.send", "vars.soldierHealth", "60");
            this.ExecuteCommand("procon.protected.send", "vars.playerRespawnTime", "100");
            this.ExecuteCommand("procon.protected.send", "vars.onlySquadLeaderSpawn", "true");

            // Remaining Variables Set From User's Default Variable List
            for (int i = 13; i < defaultVariables.Count; i++)
            {
                if (defaultVariables[i] != "YOUR_SERVER_NAME_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_MAX_PLAYERS_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_SERVER PASSWORD_HERE (Skip to Ignore)")
                    this.ExecuteCommand("procon.protected.send", variableNames[i], defaultVariables[i]);
            }
        }

        private void setCustomMode(int mapIndex)
        {
            // User's Map Specific Custom Variables
            for (int i = 0; i < variableNames.Count; i++)
            {
                if (customVariables[mapIndex][i] != "Enter Value Here! (Skip to Ignore)")
                {
                    this.ExecuteCommand("procon.protected.send", variableNames[i], customVariables[mapIndex][i]);
                    variableHasBeenSet.Add(true);
                }
                else
                {
                    variableHasBeenSet.Add(false);
                }
            }

            // Remaining Variables Set From User's Default Variable List
            for (int i = 0; i < variableNames.Count; i++)
            {
                if (!variableHasBeenSet[i])
                {
                    if (defaultVariables[i] != "YOUR_SERVER_NAME_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_MAX_PLAYERS_HERE (Skip to Ignore)" && defaultVariables[i] != "YOUR_SERVER PASSWORD_HERE (Skip to Ignore)")
                        this.ExecuteCommand("procon.protected.send", variableNames[i], defaultVariables[i]);
                }
            }

            variableHasBeenSet.Clear();
        }

        public void setVariableList()
        {
            variableNames.Add("vars.autoBalance");
            variableNames.Add("vars.friendlyFire");
            variableNames.Add("vars.killCam");
            variableNames.Add("vars.miniMap");
            variableNames.Add("vars.hud");
            variableNames.Add("vars.3dSpotting");
            variableNames.Add("vars.3pCam");
            variableNames.Add("vars.nameTag");
            variableNames.Add("vars.regenerateHealth");
            variableNames.Add("vars.vehicleSpawnAllowed");
            variableNames.Add("vars.soldierHealth");
            variableNames.Add("vars.playerRespawnTime");
            variableNames.Add("vars.onlySquadLeaderSpawn");
            variableNames.Add("vars.roundStartPlayerCount");
            variableNames.Add("vars.roundRestartPlayerCount");
            variableNames.Add("vars.gameModeCounter");
            variableNames.Add("vars.teamKillCountForKick");
            variableNames.Add("vars.teamKillValueForKick");
            variableNames.Add("vars.teamKillValueIncrease");
            variableNames.Add("vars.teamKillValueDecreasePerSecond");
            variableNames.Add("vars.teamKillKickForBan");
            variableNames.Add("vars.idleTimeout");
            variableNames.Add("vars.idleBanRounds");
            variableNames.Add("vars.serverName");
            variableNames.Add("vars.maxPlayers");
            variableNames.Add("vars.gamePassword");
            variableNames.Add("vars.vehicleSpawnDelay");
        }

        public void setDefaultVariables()
        {
            defaultVariables.Add("true");
            defaultVariables.Add("false");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("true");
            defaultVariables.Add("100");
            defaultVariables.Add("100");
            defaultVariables.Add("false");
            defaultVariables.Add("1");
            defaultVariables.Add("1");
            defaultVariables.Add("100");
            defaultVariables.Add("5");
            defaultVariables.Add("4.1");
            defaultVariables.Add("1");
            defaultVariables.Add("0.05");
            defaultVariables.Add("0");
            defaultVariables.Add("300");
            defaultVariables.Add("0");
            defaultVariables.Add("YOUR_SERVER_NAME_HERE (Skip to Ignore)");
            defaultVariables.Add("YOUR_MAX_PLAYERS_HERE (Skip to Ignore)");
            defaultVariables.Add("YOUR_SERVER PASSWORD_HERE (Skip to Ignore)");
            defaultVariables.Add("100");

            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("false");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("true");
            defaultVariablesBackup.Add("100");
            defaultVariablesBackup.Add("100");
            defaultVariablesBackup.Add("false");
            defaultVariablesBackup.Add("1");
            defaultVariablesBackup.Add("1");
            defaultVariablesBackup.Add("100");
            defaultVariablesBackup.Add("5");
            defaultVariablesBackup.Add("4.1");
            defaultVariablesBackup.Add("1");
            defaultVariablesBackup.Add("0.05");
            defaultVariablesBackup.Add("0");
            defaultVariablesBackup.Add("300");
            defaultVariablesBackup.Add("0");
            defaultVariablesBackup.Add("YOUR_SERVER_NAME_HERE (Skip to Ignore)");
            defaultVariablesBackup.Add("YOUR_MAX_PLAYERS_HERE (Skip to Ignore)");
            defaultVariablesBackup.Add("YOUR_SERVER PASSWORD_HERE (Skip to Ignore)");
            defaultVariablesBackup.Add("100");
        }

        public List<string> getListOfDefaults()
        {
            List<string> myListReturn = new List<string>();

            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");
            myListReturn.Add("Enter Value Here! (Skip to Ignore)");

            return myListReturn;
        }

        public void getMapIndices()
        {
            this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
        }

        public void getMapRounds()
        {
            this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
        }

        public void getMapList()
        {
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void initializeMapList()
        {
            myMapList.Clear();
            gameMode.Clear();
            numRounds.Clear();
            customVariables.Clear();

            needsMapInitialization = true;
            getMapList();
        }

        public CSettingChangeOnMap()
        {
            this.myMapList = new List<string>();
            this.gameMode = new List<string>();
            this.numRounds = new List<int>();
            this.variableNames = new List<string>();
            this.customVariables = new List<List<string>>();
            this.defaultVariables = new List<string>();
            this.defaultVariablesBackup = new List<string>();

            this.cameFromRunNext = false;
            this.needsMapInitialization = true;
            this.needsModeChange = false;
            this.variableHasBeenSet = new List<bool>();
            this.refreshMapList = enumBoolYesNo.No;

            this.setVariableList();
            this.setDefaultVariables();
        }

        public string GetPluginName()
        {
            return "Server Settings Change On Map";
        }

        public string GetPluginVersion()
        {
            return "1.4.5";
        }

        public string GetPluginAuthor()
        {
            return "TimSad";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/showthread.php?3388-Settings-Change-on-Specific-Map-%281.1.0-12-13-2011%29-BF3";
        }

        public string GetPluginDescription()
        {
            return @"<h2>Description</h2>
        <p>This plugin allows you to set different game modes and custom variables throughout your map rotation.  You have the option of Normal Mode, Infantry Only Mode, Hardcore Mode, and Custom Settings mode.</p>
        <h2>Settings</h2>
        <p><b>Default Variables (Server Startup Variables)</b> - Set each of these to match whatever your server starts up with using the Startup.txt file. Each one starts with their default values in their fields. 
        This is used to change the unspecified variables <i>back</i> to your server's startup values. For example, when you leave a custom variable field blank for one map, it won't use the same value that was defined for the previous map 
        and will instead revert it back to default per this Default Variables list.</p>
        <p><b>Get/Refresh Your Map List?</b> - Use this to load each of your maps from your map list into a variable to assign a game mode to. (Plugin must be enabled for this to work)</p>
        <p>Set your maps under <b>Your Map Rotation</b> to whichever game settings you would like to be played for that map.</p>
        ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnMaplistGetMapIndices", "OnRoundOver", "OnMaplistGetRounds", "OnRunNextLevel", "OnMaplistList");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bServer Settings Change On Map ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bServer Settings Change On Map ^1Disabled =(");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            for (int i = 0; i < defaultVariables.Count; i++)
            {
                lstReturn.Add(new CPluginVariable("Default Variables (Server Startup Variables)|" + variableNames[i], defaultVariables[i].GetType(), defaultVariables[i]));
            }

            lstReturn.Add(new CPluginVariable("Refresh Map List|Get/Refresh Your Map List?", typeof(enumBoolYesNo), refreshMapList));

            for (int i = 0; i < myMapList.Count; i++)
            {
                lstReturn.Add(new CPluginVariable("Your Map Rotation|" + myMapList[i], "enum.Actions(Normal Mode|Infantry Only Mode|Hardcore Mode|Custom Settings)", gameMode[i]));
            }

            for (int i = 0; i < myMapList.Count; i++)
            {
                if (gameMode[i] == "Custom Settings")
                {
                    for (int j = 0; j < this.variableNames.Count; j++)
                    {
                        lstReturn.Add(new CPluginVariable("Custom Variables: " + myMapList[i] + "|" + "Map #" + ((i + 1).ToString()) + ": " + this.variableNames[j], customVariables[i][j].GetType(), customVariables[i][j]));
                    }
                }
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            for (int i = 0; i < defaultVariables.Count; i++)
            {
                lstReturn.Add(new CPluginVariable(variableNames[i], defaultVariables[i].GetType(), defaultVariables[i]));
            }

            lstReturn.Add(new CPluginVariable("Get/Refresh Your Map List?", typeof(enumBoolYesNo), refreshMapList));

            for (int i = 0; i < myMapList.Count; i++)
            {
                lstReturn.Add(new CPluginVariable(myMapList[i], "enum.Actions(Normal Mode|Infantry Only Mode|Hardcore Mode|Custom Settings)", gameMode[i]));
            }

            for (int i = 0; i < myMapList.Count; i++)
            {
                if (gameMode[i] == "Custom Settings")
                {
                    for (int j = 0; j < variableNames.Count; j++)
                    {
                        lstReturn.Add(new CPluginVariable("Map #" + ((i + 1).ToString()) + ": " + this.variableNames[j], customVariables[i][j].GetType(), customVariables[i][j]));
                    }
                }
            }

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            for (int i = 0; i < defaultVariables.Count; i++)
            {
                if (strVariable.CompareTo(variableNames[i]) == 0)
                {
                    defaultVariables[i] = strValue;

                    if (defaultVariables[i] == "")
                        defaultVariables[i] = defaultVariablesBackup[i];
                }
            }

            if (strVariable.CompareTo("Get/Refresh Your Map List?") == 0)
            {
                if (strValue == "Yes")
                {
                    initializeMapList();
                    Thread.Sleep(200);
                }
            }

            for (int i = 0; i < myMapList.Count; i++)
            {
                if (strVariable.CompareTo(myMapList[i]) == 0)
                {
                    gameMode[i] = strValue;
                }
            }

            for (int i = 0; i < myMapList.Count; i++)
            {
                if (gameMode[i] == "Custom Settings")
                {
                    for (int j = 0; j < variableNames.Count; j++)
                    {
                        if (strVariable.CompareTo("Map #" + ((i + 1).ToString()) + ": " + this.variableNames[j]) == 0)
                        {
                            customVariables[i][j] = strValue;

                            if (customVariables[i][j] == "")
                                customVariables[i][j] = "Enter Value Here! (Skip to Ignore)";
                        }
                    }
                }
            }
        }

        public override void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            if (needsMapInitialization)
            {
                int mapNumber = 0;

                for (int i = 0; i < lstMaplist.Count; i++)
                {
                    mapNumber++;
                    numRounds.Add(lstMaplist[i].Rounds);

                    if (lstMaplist[i].MapFileName == "MP_Subway")
                    {
                        myMapList.Add("Operation Metro - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_001")
                    {
                        myMapList.Add("Grand Bazaar - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_003")
                    {
                        myMapList.Add("Teheran Highway - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_007")
                    {
                        myMapList.Add("Caspian Border - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_011")
                    {
                        myMapList.Add("Seine Crossing - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_012")
                    {
                        myMapList.Add("Operation Firestorm - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_013")
                    {
                        myMapList.Add("Damavand Peak - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_017")
                    {
                        myMapList.Add("Noshahr Canals - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "MP_018")
                    {
                        myMapList.Add("Kharg Island - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "XP1_001")
                    {
                        myMapList.Add("Strike at Karkand - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "XP1_002")
                    {
                        myMapList.Add("Gulf of Oman - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "XP1_003")
                    {
                        myMapList.Add("Sharqi Peninsula - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                    else if (lstMaplist[i].MapFileName == "XP1_004")
                    {
                        myMapList.Add("Wake Island - Map #" + mapNumber.ToString());
                        gameMode.Add("Normal Mode");
                    }
                }

                for (int i = 0; i < myMapList.Count; i++)
                    customVariables.Add(getListOfDefaults());

                this.ExecuteCommand("procon.protected.pluginconsole.write", "Successfully Retrieved Map List!");
                needsMapInitialization = false;
            }
        }

        public void OnMaplistGetRounds(int currentRound, int totalRounds)
        {
            if (needsModeChange)
            {
                if (cameFromRunNext) // Came from OnRunNextLevel
                {
                    if (gameMode[m_mapIndex] == "Normal Mode")
                        setNormalMode();
                    else if (gameMode[m_mapIndex] == "Infantry Only Mode")
                        setInfantryMode();
                    else if (gameMode[m_mapIndex] == "Hardcore Mode")
                        setHardcoreMode();
                    else if (gameMode[m_mapIndex] == "Custom Settings")
                        setCustomMode(m_mapIndex);

                    cameFromRunNext = false;
                }
                else if (!cameFromRunNext) // Came from OnRoundOver
                {
                    if (gameMode[m_nextIndex] == "Normal Mode" && currentRound == numRounds[m_mapIndex] - 1)
                        setNormalMode();
                    else if (gameMode[m_nextIndex] == "Infantry Only Mode" && currentRound == numRounds[m_mapIndex] - 1)
                        setInfantryMode();
                    else if (gameMode[m_nextIndex] == "Hardcore Mode" && currentRound == numRounds[m_mapIndex] - 1)
                        setHardcoreMode();
                    else if (gameMode[m_nextIndex] == "Custom Settings" && currentRound == numRounds[m_mapIndex] - 1)
                        setCustomMode(m_nextIndex);
                }

                needsModeChange = false;
            }

        }

        public void OnMaplistGetMapIndices(int mapIndex, int nextIndex)
        {
            m_mapIndex = mapIndex;
            m_nextIndex = nextIndex;
            getMapRounds();
        }

        public override void OnRoundOver(int iWinningTeamID)
        {
            needsModeChange = true;
            getMapIndices();
        }

        public override void OnRunNextLevel()
        {
            needsModeChange = true;
            cameFromRunNext = true;
            getMapIndices();
        }
    }
}