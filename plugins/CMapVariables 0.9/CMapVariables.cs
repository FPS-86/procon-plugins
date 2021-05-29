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
    public class CMapVariables : PRoConPluginAPI, IPRoConPluginInterface
    {
        private string fetchDefaultVariables;
        private List<string> defaultVariables;
        private string variable1;
        private string variable2;
        private string variable3;

        private string mode;
        private enumBoolYesNo displayChangesToConsole;
        private string getRefreshRotation;

        private string applyNormalMap;
        private string applyHardcoreMap;
        private string applyInfantryMap;
        private List<List<string>> mapVariables;
        private string applyNormalRotation;
        private string applyHardcoreRotation;
        private string applyInfantryRotation;
        private List<string> rotationPluginVariables;
        private List<List<string>> rotationVariables;

        private List<string> mapNames;
        private string mapEnums;
        private bool needDefaults;
        private bool needInput;
        private bool pluginEnabled;
        private int m_currentMapIndex;
        private int m_nextMapIndex;
        private int m_currentRound;
        private int m_totalRounds;
        private bool fromRoundOver;
        private bool fromRunNext;
        private bool fromPluginEnabled;
        private List<string> lastSetVariables;
        private bool needRotation;

        public CMapVariables()
        {
            this.fetchDefaultVariables = "...";
            this.defaultVariables = new List<string>();
            this.variable1 = "Enter Value Here!";
            this.variable2 = "Enter Value Here!";
            this.variable3 = "Enter Value Here!";

            this.mode = "Choose a mode...";
            this.displayChangesToConsole = enumBoolYesNo.Yes;
            this.getRefreshRotation = "...";

            this.applyNormalMap = "...";
            this.applyHardcoreMap = "...";
            this.applyInfantryMap = "...";
            this.mapVariables = new List<List<string>>();
            this.applyNormalRotation = "Enter Rotation Number Here!";
            this.applyHardcoreRotation = "Enter Rotation Number Here!";
            this.applyInfantryRotation = "Enter Rotation Number Here!";
            this.rotationPluginVariables = new List<string>();
            this.rotationVariables = new List<List<string>>();

            this.mapNames = new List<string>();
            this.mapEnums = "";
            this.needDefaults = false;
            this.needInput = false;
            this.pluginEnabled = false;
            this.m_currentMapIndex = 0;
            this.m_nextMapIndex = 0;
            this.m_currentRound = 0;
            this.m_totalRounds = 0;
            this.fromRoundOver = false;
            this.fromRunNext = false;
            this.fromPluginEnabled = false;
            this.lastSetVariables = new List<string>();
            this.needRotation = false;
        }

        public string GetPluginName()
        {
            return "Map Variables";
        }

        public string GetPluginVersion()
        {
            return "0.9.0.0";
        }

        public string GetPluginAuthor()
        {
            return "TimSad";
        }

        public string GetPluginWebsite()
        {
            return "";
        }

        public string GetPluginDescription()
        {
            return @"
        <h2>Description</h2>
          <p>This plugin allows you to have different variables set either throughout each individual map or throughout each location in your map rotation.</p>
        <h2>Settings</h2>
        ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnMaplistGetMapIndices", "OnRoundOver", "OnMaplistGetRounds",
                                                     "OnRunNextLevel", "OnMaplistList", "OnGlobalChat",
                                                     "OnServerName", "OnServerDescription", "OnServerMessage",
                                                     "OnMaxPlayers", "OnIdleTimeout", "OnIdleBanRounds",
                                                     "OnRoundRestartPlayerCount", "OnRoundStartPlayerCount",
                                                     "OnGameModeCounter", "OnFriendlyFire", "OnUnlockMode",
                                                     "OnTeamKillCountForKick", "OnTeamKillValueIncrease",
                                                     "OnTeamKillValueDecreasePerSecond", "OnTeamKillValueForKick",
                                                     "OnTeamBalance", "OnKillCam", "OnMiniMap", "On3dSpotting",
                                                     "OnMiniMapSpotting", "OnGunMasterWeaponsPreset",
                                                     "OnVehicleSpawnAllowed", "OnVehicleSpawnDelay", "OnBulletDamage",
                                                     "OnOnlySquadLeaderSpawn", "OnSoldierHealth", "OnPlayerManDownTime",
                                                     "OnPlayerRespawnTime", "OnHud", "OnNameTag", "OnPremiumStatus",
                                                     "OnCtfRoundTimeModifier");

            this.mapNames = new List<string>(GetMapList("{PublicLevelName} - {GameMode}"));
            this.getMapEnums();
            for (int i = 0; i < this.mapNames.Count; i++)
            {
                this.mapVariables.Add(new List<string>());
            }
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMap Variables ^2Enabled!");

            this.pluginEnabled = true;

            this.ExecuteCommand("procon.protected.tasks.add", "CMapVariablesGetMapIndexRound", "8", "1", "1", "procon.protected.plugins.call", "CMapVariables", "getMapIndexRound");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMap Variables ^1Disabled =(");

            this.pluginEnabled = false;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Default Server Variables|Fetch Default (Original) Server Variables", "enum.DefaultVariables(...|DO IT!)", this.fetchDefaultVariables));
            lstReturn.Add(new CPluginVariable("Default Server Variables|Default (Original) Variable List", typeof(string[]), this.defaultVariables.ToArray()));

            if (this.needInput)
            {
                lstReturn.Add(new CPluginVariable("Default Server Variables|vars.3pCam (True or False)", this.variable1.GetType(), this.variable1));
                lstReturn.Add(new CPluginVariable("Default Server Variables|vars.regenerateHealth (True or False)", this.variable2.GetType(), this.variable2));
                lstReturn.Add(new CPluginVariable("Default Server Variables|vars.teamKillKickForBan (0 to 4294967295)", this.variable3.GetType(), this.variable3));
            }

            lstReturn.Add(new CPluginVariable("Variables Setting Mode", "enum.Mode(Choose a mode...|Map Based|Rotation Based)", this.mode));
            lstReturn.Add(new CPluginVariable("Display Variable Changes to Console?", typeof(enumBoolYesNo), this.displayChangesToConsole));
            if (this.mode == "Map Based")
            {
                lstReturn.Add(new CPluginVariable("Maps|Apply Normal Preset Variables to Map:", "enum.ApplyNormalMap(" + this.mapEnums + ")", this.applyNormalMap));
                lstReturn.Add(new CPluginVariable("Maps|Apply Hardcore Preset Variables to Map:", "enum.ApplyHardcoreMap(" + this.mapEnums + ")", this.applyHardcoreMap));
                lstReturn.Add(new CPluginVariable("Maps|Apply Infantry Only Preset Variables to Map:", "enum.ApplyInfantryMap(" + this.mapEnums + ")", this.applyInfantryMap));

                for (int i = 0; i < this.mapNames.Count; i++)
                {
                    lstReturn.Add(new CPluginVariable("Maps|" + this.mapNames[i], typeof(string[]), this.mapVariables[i].ToArray()));
                }
            }
            else if (this.mode == "Rotation Based")
            {
                lstReturn.Add(new CPluginVariable("Get/Refresh Your Rotation?", "enum.getRotation(...|DO IT!)", this.getRefreshRotation));

                if (this.rotationPluginVariables.Count > 0)
                {
                    lstReturn.Add(new CPluginVariable("Rotation|Apply Normal Preset Variables to Rotation:", this.applyNormalRotation.GetType(), this.applyNormalRotation));
                    lstReturn.Add(new CPluginVariable("Rotation|Apply Hardcore Preset Variables to Rotation:", this.applyHardcoreRotation.GetType(), this.applyHardcoreRotation));
                    lstReturn.Add(new CPluginVariable("Rotation|Apply Infantry Only Preset Variables to Rotation:", this.applyInfantryRotation.GetType(), this.applyInfantryRotation));
                }

                for (int i = 0; i < this.rotationPluginVariables.Count; i++)
                {
                    lstReturn.Add(new CPluginVariable("Rotation|" + i.ToString() + ". " + this.rotationPluginVariables[i], typeof(string[]), this.rotationVariables[i].ToArray()));
                }
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Fetch Default (Original) Server Variables", "enum.DefaultVariables(...|DO IT!)", this.fetchDefaultVariables));
            lstReturn.Add(new CPluginVariable("Default (Original) Variable List", typeof(string[]), this.defaultVariables.ToArray()));

            if (this.needInput)
            {
                lstReturn.Add(new CPluginVariable("vars.3pCam (True or False)", this.variable1.GetType(), this.variable1));
                lstReturn.Add(new CPluginVariable("vars.regenerateHealth (True or False)", this.variable2.GetType(), this.variable2));
                lstReturn.Add(new CPluginVariable("vars.teamKillKickForBan (0 to 4294967295)", this.variable3.GetType(), this.variable3));
            }

            lstReturn.Add(new CPluginVariable("Variables Setting Mode", "enum.Mode(Choose a mode...|Map Based|Rotation Based)", this.mode));
            lstReturn.Add(new CPluginVariable("Display Variable Changes to Console?", typeof(enumBoolYesNo), this.displayChangesToConsole));
            if (this.mode == "Map Based")
            {
                lstReturn.Add(new CPluginVariable("Apply Normal Preset Variables to Map:", "enum.ApplyNormalMap(" + this.mapEnums + ")", this.applyNormalMap));
                lstReturn.Add(new CPluginVariable("Apply Hardcore Preset Variables to Map:", "enum.ApplyHardcoreMap(" + this.mapEnums + ")", this.applyHardcoreMap));
                lstReturn.Add(new CPluginVariable("Apply Infantry Only Preset Variables to Map:", "enum.ApplyInfantryMap(" + this.mapEnums + ")", this.applyInfantryMap));

                for (int i = 0; i < this.mapNames.Count; i++)
                {
                    lstReturn.Add(new CPluginVariable(this.mapNames[i], typeof(string[]), this.mapVariables[i].ToArray()));
                }
            }
            else if (this.mode == "Rotation Based")
            {
                lstReturn.Add(new CPluginVariable("Get/Refresh Your Rotation?", "enum.getRotation(...|DO IT!)", this.getRefreshRotation));

                if (this.rotationPluginVariables.Count > 0)
                {
                    lstReturn.Add(new CPluginVariable("Apply Normal Preset Variables to Rotation:", this.applyNormalRotation.GetType(), this.applyNormalRotation));
                    lstReturn.Add(new CPluginVariable("Apply Hardcore Preset Variables to Rotation:", this.applyHardcoreRotation.GetType(), this.applyHardcoreRotation));
                    lstReturn.Add(new CPluginVariable("Apply Infantry Only Preset Variables to Rotation:", this.applyInfantryRotation.GetType(), this.applyInfantryRotation));
                }

                for (int i = 0; i < this.rotationPluginVariables.Count; i++)
                {
                    lstReturn.Add(new CPluginVariable(i.ToString() + ". " + this.rotationPluginVariables[i], typeof(string[]), this.rotationVariables[i].ToArray()));
                }
            }

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Fetch Default (Original) Server Variables") == 0)
            {
                if (strValue == "DO IT!")
                {
                    if (this.pluginEnabled)
                    {
                        this.defaultVariables.Clear();
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0WORKING! ^n- Fetching current server variables...");
                        this.initializeDefaultVariables();
                        this.needInput = true;
                    }
                    else
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0NOTICE! ^n- Plugin must first be enabled to fetch your current server variables!");
                }
            }
            else if (strVariable.CompareTo("Default (Original) Variable List") == 0)
            {
                this.defaultVariables = new List<string>(CPluginVariable.DecodeStringArray(strValue));

                if (this.mode != "Choose a mode..." && !this.hasAllVariables())
                    this.mode = "Choose a mode...";

                //// Rebuild defaultVariables List on Procon launch for cases that strings have the pipe "|" symbol ////
                for (int i = 0; i < this.defaultVariables.Count; i++)
                {
                    while (this.isValidVariableLine(this.defaultVariables[i]) && !(this.isValidVariableLine(this.defaultVariables[i + 1])))
                    {
                        this.defaultVariables[i] += "|" + this.defaultVariables[i + 1];
                        this.defaultVariables.RemoveRange(i + 1, 1);
                    }
                }
            }
            else if (strVariable.CompareTo("vars.3pCam (True or False)") == 0)
            {
                if (strValue == "True" || strValue == "False")
                    this.variable1 = strValue;

                if (this.variable1 != "Enter Value Here!" && this.variable2 != "Enter Value Here!" && this.variable3 != "Enter Value Here!")
                {
                    this.defaultVariables.Add("vars.3pCam " + variable1);
                    this.defaultVariables.Add("vars.regenerateHealth " + variable2);
                    this.defaultVariables.Add("vars.teamKillKickForBan " + variable3);
                    this.variable1 = "Enter Value Here!";
                    this.variable2 = "Enter Value Here!";
                    this.variable3 = "Enter Value Here!";
                    this.needInput = false;
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0SUCCESS! ^n- Variables retrieved and stored in Default Variable List.");
                }
            }
            else if (strVariable.CompareTo("vars.regenerateHealth (True or False)") == 0)
            {
                if (strValue == "True" || strValue == "False")
                    this.variable2 = strValue;

                if (this.variable1 != "Enter Value Here!" && this.variable2 != "Enter Value Here!" && this.variable3 != "Enter Value Here!")
                {
                    this.defaultVariables.Add("vars.3pCam " + variable1);
                    this.defaultVariables.Add("vars.regenerateHealth " + variable2);
                    this.defaultVariables.Add("vars.teamKillKickForBan " + variable3);
                    this.variable1 = "Enter Value Here!";
                    this.variable2 = "Enter Value Here!";
                    this.variable3 = "Enter Value Here!";
                    this.needInput = false;
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0SUCCESS! ^n- Variables retrieved and stored in Default Variable List.");
                }
            }
            else if (strVariable.CompareTo("vars.teamKillKickForBan (0 to 4294967295)") == 0)
            {
                int strValueAsInt;
                if (Int32.TryParse(strValue, out strValueAsInt))
                    this.variable3 = strValue;

                if (this.variable1 != "Enter Value Here!" && this.variable2 != "Enter Value Here!" && this.variable3 != "Enter Value Here!")
                {
                    this.defaultVariables.Add("vars.3pCam " + variable1);
                    this.defaultVariables.Add("vars.regenerateHealth " + variable2);
                    this.defaultVariables.Add("vars.teamKillKickForBan " + variable3);
                    this.variable1 = "Enter Value Here!";
                    this.variable2 = "Enter Value Here!";
                    this.variable3 = "Enter Value Here!";
                    this.needInput = false;
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0SUCCESS! ^n- Variables retrieved and stored in Default Variable List.");
                }
            }
            else if (strVariable.CompareTo("Variables Setting Mode") == 0)
            {
                if (strValue == "Map Based" || strValue == "Rotation Based")
                {
                    if (this.hasAllVariables())
                    {
                        this.mode = strValue;
                    }
                    else
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0NOTICE! ^n- You must first have a full list of 35 default variables to continue...");
                }
            }
            else if (strVariable.CompareTo("Display Variable Changes to Console?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.displayChangesToConsole = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Get/Refresh Your Rotation?") == 0)
            {
                if (strValue == "DO IT!")
                {
                    this.getRotation();
                }
            }
            else if (strVariable.CompareTo("Apply Normal Preset Variables to Map:") == 0)
            {
                for (int i = 0; i < this.mapNames.Count; i++)
                {
                    if (strValue == this.mapNames[i])
                        this.applyMapVariables("Normal", i);
                }
            }
            else if (strVariable.CompareTo("Apply Hardcore Preset Variables to Map:") == 0)
            {
                for (int i = 0; i < this.mapNames.Count; i++)
                {
                    if (strValue == this.mapNames[i])
                        this.applyMapVariables("Hardcore", i);
                }
            }
            else if (strVariable.CompareTo("Apply Infantry Only Preset Variables to Map:") == 0)
            {
                for (int i = 0; i < this.mapNames.Count; i++)
                {
                    if (strValue == this.mapNames[i])
                        this.applyMapVariables("Infantry", i);
                }
            }
            else if (strVariable.CompareTo("Apply Normal Preset Variables to Rotation:") == 0)
            {
                int strValueAsInt;
                if (Int32.TryParse(strValue, out strValueAsInt))
                {
                    if (strValueAsInt <= this.rotationPluginVariables.Count - 1)
                        this.applyRotationVariables("Normal", strValueAsInt);
                    else
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0NOTICE! ^n- Your rotation does not exceed " + (this.rotationPluginVariables.Count - 1).ToString() + ".");
                }
            }
            else if (strVariable.CompareTo("Apply Hardcore Preset Variables to Rotation:") == 0)
            {
                int strValueAsInt;
                if (Int32.TryParse(strValue, out strValueAsInt))
                {
                    if (strValueAsInt <= this.rotationPluginVariables.Count - 1)
                        this.applyRotationVariables("Hardcore", strValueAsInt);
                    else
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0NOTICE! ^n- Your rotation does not exceed " + (this.rotationPluginVariables.Count - 1).ToString() + ".");
                }
            }
            else if (strVariable.CompareTo("Apply Infantry Only Preset Variables to Rotation:") == 0)
            {
                int strValueAsInt;
                if (Int32.TryParse(strValue, out strValueAsInt))
                {
                    if (strValueAsInt <= this.rotationPluginVariables.Count - 1)
                        this.applyRotationVariables("Infantry", strValueAsInt);
                    else
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0NOTICE! ^n- Your rotation does not exceed " + (this.rotationPluginVariables.Count - 1).ToString() + ".");
                }
            }

            for (int i = 0; i < this.mapNames.Count; i++)
            {
                if (strVariable.CompareTo(this.mapNames[i]) == 0)
                {
                    this.mapVariables[i] = new List<string>(CPluginVariable.DecodeStringArray(strValue));

                    for (int j = 0; j < this.mapVariables[i].Count; j++)
                    {
                        if (this.mapVariables[i][j].EndsWith(@""""""))
                        {
                            this.mapVariables[i][j] = this.mapVariables[i][j].Substring(0, this.mapVariables[i][j].Length - 1);
                        }
                    }

                    //// Rebuild mapVariables[i] List on Procon launch for cases that strings have the pipe "|" symbol ////
                    for (int j = 0; j < this.mapVariables[i].Count; j++)
                    {
                        while (this.isValidVariableLine(this.mapVariables[i][j]) && !(this.isValidVariableLine(this.mapVariables[i][j + 1])) && !this.mapVariables[i][j + 1].StartsWith("///"))
                        {
                            this.mapVariables[i][j] += "|" + this.mapVariables[i][j + 1];
                            this.mapVariables[i].RemoveRange(j + 1, 1);
                        }

                        if (this.mapVariables[i][j] == "")
                            this.mapVariables[i].RemoveRange(j, 1);
                    }
                }
            }

            for (int i = 0; i < this.rotationPluginVariables.Count; i++)
            {
                if (strVariable.CompareTo(i.ToString() + ". " + this.rotationPluginVariables[i]) == 0)
                {
                    this.rotationVariables[i] = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                }
            }
        }

        public void getMapIndexRound()
        {
            this.m_currentRound++; // add 1 to current round for non-index based number
            this.fromPluginEnabled = true;
            this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
        }

        private void getRotation()
        {
            this.needRotation = true;
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        private bool isValidVariableLine(string line)
        {
            if (line.StartsWith("vars."))
                return true;

            return false;
        }

        private bool hasAllVariables()
        {
            List<string> bf3Variables = new List<string>();

            bf3Variables.Add("vars.serverName");
            bf3Variables.Add("vars.serverDescription");
            bf3Variables.Add("vars.serverMessage");
            bf3Variables.Add("vars.maxPlayers");
            bf3Variables.Add("vars.idleTimeout");
            bf3Variables.Add("vars.idleBanRounds");
            bf3Variables.Add("vars.roundRestartPlayerCount");
            bf3Variables.Add("vars.roundStartPlayerCount");
            bf3Variables.Add("vars.gameModeCounter");
            bf3Variables.Add("vars.friendlyFire");
            bf3Variables.Add("vars.unlockMode");
            bf3Variables.Add("vars.teamKillCountForKick");
            bf3Variables.Add("vars.teamKillValueIncrease");
            bf3Variables.Add("vars.teamKillValueDecreasePerSecond");
            bf3Variables.Add("vars.teamKillValueForKick");
            bf3Variables.Add("vars.autoBalance");
            bf3Variables.Add("vars.killCam");
            bf3Variables.Add("vars.miniMap");
            bf3Variables.Add("vars.3dSpotting");
            bf3Variables.Add("vars.miniMapSpotting");
            bf3Variables.Add("vars.gunMasterWeaponsPreset");
            bf3Variables.Add("vars.vehicleSpawnAllowed");
            bf3Variables.Add("vars.vehicleSpawnDelay");
            bf3Variables.Add("vars.bulletDamage");
            bf3Variables.Add("vars.onlySquadLeaderSpawn");
            bf3Variables.Add("vars.soldierHealth");
            bf3Variables.Add("vars.playerManDownTime");
            bf3Variables.Add("vars.playerRespawnTime");
            bf3Variables.Add("vars.nameTag");
            bf3Variables.Add("vars.hud");
            bf3Variables.Add("vars.premiumStatus");
            bf3Variables.Add("vars.ctfRoundTimeModifier");
            bf3Variables.Add("vars.3pCam");
            bf3Variables.Add("vars.regenerateHealth");
            bf3Variables.Add("vars.teamKillKickForBan");

            for (int i = 0; i < bf3Variables.Count; i++)
            {
                bool hasVariable = false;

                for (int j = 0; j < this.defaultVariables.Count; j++)
                {
                    if (this.defaultVariables[j].StartsWith(bf3Variables[i]))
                        hasVariable = true;
                }

                if (!hasVariable)
                    return false;
            }

            return true;
        }

        private void getMapEnums()
        {
            this.mapEnums = "...|";

            for (int i = 0; i < this.mapNames.Count; i++)
                this.mapEnums += this.mapNames[i] + "|";

            this.mapEnums = this.mapEnums.TrimEnd('|');
        }

        private void applyMapVariables(string preset, int mapModeIndex)
        {
            if (this.mapVariables[mapModeIndex].Count > 0)
            {
                string[] presetVars = new string[18];
                presetVars[0] = "vars.autoBalance";
                presetVars[1] = "vars.friendlyFire";
                presetVars[2] = "vars.killCam";
                presetVars[3] = "vars.miniMap";
                presetVars[4] = "vars.hud";
                presetVars[5] = "vars.3dSpotting";
                presetVars[6] = "vars.miniMapSpotting";
                presetVars[7] = "vars.nameTag";
                presetVars[8] = "vars.3pCam";
                presetVars[9] = "vars.regenerateHealth";
                presetVars[10] = "vars.vehicleSpawnAllowed";
                presetVars[11] = "vars.soldierHealth";
                presetVars[12] = "vars.playerRespawnTime";
                presetVars[13] = "vars.playerManDownTime";
                presetVars[14] = "vars.bulletDamage";
                presetVars[15] = "vars.onlySquadLeaderSpawn";
                presetVars[16] = "Preset Variables";
                presetVars[17] = "/// End of";

                for (int i = 0; i < 18; i++)
                {
                    for (int j = 0; j < this.mapVariables[mapModeIndex].Count; j++)
                    {
                        if (this.mapVariables[mapModeIndex][j].Contains(presetVars[i]))
                        {
                            this.mapVariables[mapModeIndex].RemoveAt(j);
                        }
                    }
                }
            }

            if (preset == "Normal")
            {
                this.mapVariables[mapModeIndex].Add("///// Normal Preset Variables /////");
                this.mapVariables[mapModeIndex].Add("vars.autoBalance True");
                this.mapVariables[mapModeIndex].Add("vars.friendlyFire False");
                this.mapVariables[mapModeIndex].Add("vars.killCam True");
                this.mapVariables[mapModeIndex].Add("vars.miniMap True");
                this.mapVariables[mapModeIndex].Add("vars.hud True");
                this.mapVariables[mapModeIndex].Add("vars.3dSpotting True");
                this.mapVariables[mapModeIndex].Add("vars.miniMapSpotting True");
                this.mapVariables[mapModeIndex].Add("vars.nameTag True");
                this.mapVariables[mapModeIndex].Add("vars.3pCam True");
                this.mapVariables[mapModeIndex].Add("vars.regenerateHealth True");
                this.mapVariables[mapModeIndex].Add("vars.vehicleSpawnAllowed True");
                this.mapVariables[mapModeIndex].Add("vars.soldierHealth 100");
                this.mapVariables[mapModeIndex].Add("vars.playerRespawnTime 100");
                this.mapVariables[mapModeIndex].Add("vars.playerManDownTime 100");
                this.mapVariables[mapModeIndex].Add("vars.bulletDamage 100");
                this.mapVariables[mapModeIndex].Add("vars.onlySquadLeaderSpawn False");
                this.mapVariables[mapModeIndex].Add("/// End of Normal Preset Variables ///");

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0Normal Preset Variables applied to ^b" + this.mapNames[mapModeIndex]);
            }
            else if (preset == "Hardcore")
            {
                this.mapVariables[mapModeIndex].Add("///// Hardcore Preset Variables /////");
                this.mapVariables[mapModeIndex].Add("vars.autoBalance True");
                this.mapVariables[mapModeIndex].Add("vars.friendlyFire True");
                this.mapVariables[mapModeIndex].Add("vars.killCam False");
                this.mapVariables[mapModeIndex].Add("vars.miniMap True");
                this.mapVariables[mapModeIndex].Add("vars.hud False");
                this.mapVariables[mapModeIndex].Add("vars.3dSpotting False");
                this.mapVariables[mapModeIndex].Add("vars.miniMapSpotting True");
                this.mapVariables[mapModeIndex].Add("vars.nameTag False");
                this.mapVariables[mapModeIndex].Add("vars.3pCam False");
                this.mapVariables[mapModeIndex].Add("vars.regenerateHealth False");
                this.mapVariables[mapModeIndex].Add("vars.vehicleSpawnAllowed True");
                this.mapVariables[mapModeIndex].Add("vars.soldierHealth 60");
                this.mapVariables[mapModeIndex].Add("vars.playerRespawnTime 100");
                this.mapVariables[mapModeIndex].Add("vars.playerManDownTime 100");
                this.mapVariables[mapModeIndex].Add("vars.bulletDamage 100");
                this.mapVariables[mapModeIndex].Add("vars.onlySquadLeaderSpawn True");
                this.mapVariables[mapModeIndex].Add("/// End of Hardcore Preset Variables ///");

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0Hardcore Preset Variables applied to ^b" + this.mapNames[mapModeIndex]);
            }
            else if (preset == "Infantry")
            {
                this.mapVariables[mapModeIndex].Add("///// Infanty Only Preset Variables /////");
                this.mapVariables[mapModeIndex].Add("vars.autoBalance True");
                this.mapVariables[mapModeIndex].Add("vars.friendlyFire False");
                this.mapVariables[mapModeIndex].Add("vars.killCam True");
                this.mapVariables[mapModeIndex].Add("vars.miniMap True");
                this.mapVariables[mapModeIndex].Add("vars.hud True");
                this.mapVariables[mapModeIndex].Add("vars.3dSpotting True");
                this.mapVariables[mapModeIndex].Add("vars.miniMapSpotting True");
                this.mapVariables[mapModeIndex].Add("vars.nameTag True");
                this.mapVariables[mapModeIndex].Add("vars.3pCam False");
                this.mapVariables[mapModeIndex].Add("vars.regenerateHealth True");
                this.mapVariables[mapModeIndex].Add("vars.vehicleSpawnAllowed False");
                this.mapVariables[mapModeIndex].Add("vars.soldierHealth 100");
                this.mapVariables[mapModeIndex].Add("vars.playerRespawnTime 100");
                this.mapVariables[mapModeIndex].Add("vars.playerManDownTime 100");
                this.mapVariables[mapModeIndex].Add("vars.bulletDamage 100");
                this.mapVariables[mapModeIndex].Add("vars.onlySquadLeaderSpawn False");
                this.mapVariables[mapModeIndex].Add("/// End of Infanty Only Preset Variables ///");

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0Infanty Only Preset Variables applied to ^b" + this.mapNames[mapModeIndex]);
            }
        }

        private void applyRotationVariables(string preset, int rotationIndex)
        {
            if (this.rotationVariables[rotationIndex].Count > 0)
            {
                string[] presetVars = new string[18];
                presetVars[0] = "vars.autoBalance";
                presetVars[1] = "vars.friendlyFire";
                presetVars[2] = "vars.killCam";
                presetVars[3] = "vars.miniMap";
                presetVars[4] = "vars.hud";
                presetVars[5] = "vars.3dSpotting";
                presetVars[6] = "vars.miniMapSpotting";
                presetVars[7] = "vars.nameTag";
                presetVars[8] = "vars.3pCam";
                presetVars[9] = "vars.regenerateHealth";
                presetVars[10] = "vars.vehicleSpawnAllowed";
                presetVars[11] = "vars.soldierHealth";
                presetVars[12] = "vars.playerRespawnTime";
                presetVars[13] = "vars.playerManDownTime";
                presetVars[14] = "vars.bulletDamage";
                presetVars[15] = "vars.onlySquadLeaderSpawn";
                presetVars[16] = "Preset Variables";
                presetVars[17] = "/// End of";

                for (int i = 0; i < 18; i++)
                {
                    for (int j = 0; j < this.rotationVariables[rotationIndex].Count; j++)
                    {
                        if (this.rotationVariables[rotationIndex][j].Contains(presetVars[i]))
                        {
                            this.rotationVariables[rotationIndex].RemoveAt(j);
                        }
                    }
                }
            }

            if (preset == "Normal")
            {
                this.rotationVariables[rotationIndex].Add("///// Normal Preset Variables /////");
                this.rotationVariables[rotationIndex].Add("vars.autoBalance True");
                this.rotationVariables[rotationIndex].Add("vars.friendlyFire False");
                this.rotationVariables[rotationIndex].Add("vars.killCam True");
                this.rotationVariables[rotationIndex].Add("vars.miniMap True");
                this.rotationVariables[rotationIndex].Add("vars.hud True");
                this.rotationVariables[rotationIndex].Add("vars.3dSpotting True");
                this.rotationVariables[rotationIndex].Add("vars.miniMapSpotting True");
                this.rotationVariables[rotationIndex].Add("vars.nameTag True");
                this.rotationVariables[rotationIndex].Add("vars.3pCam True");
                this.rotationVariables[rotationIndex].Add("vars.regenerateHealth True");
                this.rotationVariables[rotationIndex].Add("vars.vehicleSpawnAllowed True");
                this.rotationVariables[rotationIndex].Add("vars.soldierHealth 100");
                this.rotationVariables[rotationIndex].Add("vars.playerRespawnTime 100");
                this.rotationVariables[rotationIndex].Add("vars.playerManDownTime 100");
                this.rotationVariables[rotationIndex].Add("vars.bulletDamage 100");
                this.rotationVariables[rotationIndex].Add("vars.onlySquadLeaderSpawn False");
                this.rotationVariables[rotationIndex].Add("/// End of Normal Preset Variables ///");

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0Normal Preset Variables applied to ^b" + this.rotationPluginVariables[rotationIndex]);
            }
            else if (preset == "Hardcore")
            {
                this.rotationVariables[rotationIndex].Add("///// Hardcore Preset Variables /////");
                this.rotationVariables[rotationIndex].Add("vars.autoBalance True");
                this.rotationVariables[rotationIndex].Add("vars.friendlyFire True");
                this.rotationVariables[rotationIndex].Add("vars.killCam False");
                this.rotationVariables[rotationIndex].Add("vars.miniMap True");
                this.rotationVariables[rotationIndex].Add("vars.hud False");
                this.rotationVariables[rotationIndex].Add("vars.3dSpotting False");
                this.rotationVariables[rotationIndex].Add("vars.miniMapSpotting True");
                this.rotationVariables[rotationIndex].Add("vars.nameTag False");
                this.rotationVariables[rotationIndex].Add("vars.3pCam False");
                this.rotationVariables[rotationIndex].Add("vars.regenerateHealth False");
                this.rotationVariables[rotationIndex].Add("vars.vehicleSpawnAllowed True");
                this.rotationVariables[rotationIndex].Add("vars.soldierHealth 60");
                this.rotationVariables[rotationIndex].Add("vars.playerRespawnTime 100");
                this.rotationVariables[rotationIndex].Add("vars.playerManDownTime 100");
                this.rotationVariables[rotationIndex].Add("vars.bulletDamage 100");
                this.rotationVariables[rotationIndex].Add("vars.onlySquadLeaderSpawn True");
                this.rotationVariables[rotationIndex].Add("/// End of Hardcore Preset Variables ///");

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0Hardcore Preset Variables applied to ^b" + this.rotationPluginVariables[rotationIndex]);
            }
            else if (preset == "Infantry")
            {
                this.rotationVariables[rotationIndex].Add("///// Infanty Only Preset Variables /////");
                this.rotationVariables[rotationIndex].Add("vars.autoBalance True");
                this.rotationVariables[rotationIndex].Add("vars.friendlyFire False");
                this.rotationVariables[rotationIndex].Add("vars.killCam True");
                this.rotationVariables[rotationIndex].Add("vars.miniMap True");
                this.rotationVariables[rotationIndex].Add("vars.hud True");
                this.rotationVariables[rotationIndex].Add("vars.3dSpotting True");
                this.rotationVariables[rotationIndex].Add("vars.miniMapSpotting True");
                this.rotationVariables[rotationIndex].Add("vars.nameTag True");
                this.rotationVariables[rotationIndex].Add("vars.3pCam False");
                this.rotationVariables[rotationIndex].Add("vars.regenerateHealth True");
                this.rotationVariables[rotationIndex].Add("vars.vehicleSpawnAllowed False");
                this.rotationVariables[rotationIndex].Add("vars.soldierHealth 100");
                this.rotationVariables[rotationIndex].Add("vars.playerRespawnTime 100");
                this.rotationVariables[rotationIndex].Add("vars.playerManDownTime 100");
                this.rotationVariables[rotationIndex].Add("vars.bulletDamage 100");
                this.rotationVariables[rotationIndex].Add("vars.onlySquadLeaderSpawn False");
                this.rotationVariables[rotationIndex].Add("/// End of Infanty Only Preset Variables ///");

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0Infanty Only Preset Variables applied to ^b" + this.rotationPluginVariables[rotationIndex]);
            }
        }

        public void initializeDefaultVariables()
        {
            List<string> bf3Variables = new List<string>();

            bf3Variables.Add("vars.serverName");
            bf3Variables.Add("vars.serverDescription");
            bf3Variables.Add("vars.serverMessage");
            bf3Variables.Add("vars.maxPlayers");
            bf3Variables.Add("vars.idleTimeout");
            bf3Variables.Add("vars.idleBanRounds");
            bf3Variables.Add("vars.roundRestartPlayerCount");
            bf3Variables.Add("vars.roundStartPlayerCount");
            bf3Variables.Add("vars.gameModeCounter");
            bf3Variables.Add("vars.friendlyFire");
            bf3Variables.Add("vars.unlockMode");
            bf3Variables.Add("vars.teamKillCountForKick");
            bf3Variables.Add("vars.teamKillValueIncrease");
            bf3Variables.Add("vars.teamKillValueDecreasePerSecond");
            bf3Variables.Add("vars.teamKillValueForKick");
            bf3Variables.Add("vars.autoBalance");
            bf3Variables.Add("vars.killCam");
            bf3Variables.Add("vars.miniMap");
            bf3Variables.Add("vars.3dSpotting");
            bf3Variables.Add("vars.miniMapSpotting");
            bf3Variables.Add("vars.gunMasterWeaponsPreset");
            bf3Variables.Add("vars.vehicleSpawnAllowed");
            bf3Variables.Add("vars.vehicleSpawnDelay");
            bf3Variables.Add("vars.bulletDamage");
            bf3Variables.Add("vars.onlySquadLeaderSpawn");
            bf3Variables.Add("vars.soldierHealth");
            bf3Variables.Add("vars.playerManDownTime");
            bf3Variables.Add("vars.playerRespawnTime");
            bf3Variables.Add("vars.nameTag");
            bf3Variables.Add("vars.hud");
            bf3Variables.Add("vars.premiumStatus");
            bf3Variables.Add("vars.ctfRoundTimeModifier");

            this.needDefaults = true;

            for (int i = 0; i < bf3Variables.Count; i++)
            {
                this.ExecuteCommand("procon.protected.send", bf3Variables[i]);
            }
        }

        private string getPublicGameModeName(string gameMode)
        {
            if (gameMode == "ConquestLarge0")
                return "Conquest Large";
            else if (gameMode == "ConquestSmall0")
                return "Conquest Small";
            else if (gameMode == "ConquestAssaultLarge0")
                return "Assault64";
            else if (gameMode == "ConquestAssaultSmall0")
                return "Assault";
            else if (gameMode == "ConquestAssaultSmall1")
                return "Assault #2";
            else if (gameMode == "RushLarge0")
                return "Rush";
            else if (gameMode == "SquadRush0")
                return "Squad Rush";
            else if (gameMode == "SquadDeathMatch0")
                return "Squad Deathmatch";
            else if (gameMode == "TeamDeathMatch0")
                return "TDM";
            else if (gameMode == "TeamDeathMatchC0")
                return "TDM Close Quarters";
            else if (gameMode == "Domination0")
                return "Conquest Domination";
            else if (gameMode == "GunMaster0")
                return "Gun Master";
            else if (gameMode == "TankSuperiority0")
                return "Tank Superiority";
            else if (gameMode == "Scavenger0")
                return "Scavenger";
            else if (gameMode == "CaptureTheFlag0")
                return "CTF";
            else if (gameMode == "AirSuperiority0")
                return "Air Superiority";
            else
                return "Unknown Game Mode";
        }

        private void setServerVariables(string mapModeString)
        {
            List<string> tempVariableList = new List<string>();

            for (int i = 0; i < this.mapNames.Count; i++)
            {
                if (this.mapNames[i] == mapModeString)
                {
                    if (this.mapVariables[i].Count > 0)
                    {
                        for (int j = 0; j < this.mapVariables[i].Count; j++)
                        {
                            if (this.mapVariables[i][j].StartsWith("vars."))
                            {
                                string[] variableValueArr = this.mapVariables[i][j].Replace(@"""", "").Split(' ');
                                if (variableValueArr.Length > 2)
                                {
                                    for (int k = 2; k < variableValueArr.Length; k++)
                                        variableValueArr[1] += " " + variableValueArr[k];
                                }

                                this.ExecuteCommand("procon.protected.send", variableValueArr[0], variableValueArr[1]);
                                tempVariableList.Add(variableValueArr[0]);

                                if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0" + variableValueArr[0] + " " + variableValueArr[1]);
                            }
                        }
                    }
                    else if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0No Applicable Variables to Be Set!");
                }
            }

            if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0Reverting Following Variables Back to Default...");

            bool variableReset = false;
            for (int i = 0; i < this.defaultVariables.Count; i++)
            {
                string[] variableValueArr = this.defaultVariables[i].Replace(@"""", "").Split(' ');
                if (variableValueArr.Length > 2)
                {
                    for (int j = 2; j < variableValueArr.Length; j++)
                        variableValueArr[1] += " " + variableValueArr[j];
                }

                if (this.lastSetVariables.Contains(variableValueArr[0]) && !tempVariableList.Contains(variableValueArr[0]))
                {
                    this.ExecuteCommand("procon.protected.send", variableValueArr[0], variableValueArr[1]);

                    if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0" + variableValueArr[0] + " " + variableValueArr[1]);

                    variableReset = true;
                }
            }

            if (!variableReset)
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0No Variables Needed Reverting!");

            this.lastSetVariables = tempVariableList;
        }

        private void setServerVariablesRotation(int mapIndex)
        {
            List<string> tempVariableList = new List<string>();

            if (this.rotationVariables[mapIndex].Count > 0)
            {
                for (int i = 0; i < this.rotationVariables[mapIndex].Count; i++)
                {
                    if (this.rotationVariables[mapIndex][i].StartsWith("vars."))
                    {
                        string[] variableValueArr = this.rotationVariables[mapIndex][i].Replace(@"""", "").Split(' ');
                        if (variableValueArr.Length > 2)
                        {
                            for (int j = 2; j < variableValueArr.Length; j++)
                                variableValueArr[1] += " " + variableValueArr[j];
                        }

                        this.ExecuteCommand("procon.protected.send", variableValueArr[0], variableValueArr[1]);
                        tempVariableList.Add(variableValueArr[0]);

                        if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0" + variableValueArr[0] + " " + variableValueArr[1]);
                    }
                }
            }
            else if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0No Applicable Variables to Be Set!");

            if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0Reverting Following Variables Back to Default...");

            bool variableReset = false;
            for (int i = 0; i < this.defaultVariables.Count; i++)
            {
                string[] variableValueArr = this.defaultVariables[i].Replace(@"""", "").Split(' ');
                if (variableValueArr.Length > 2)
                {
                    for (int j = 2; j < variableValueArr.Length; j++)
                        variableValueArr[1] += " " + variableValueArr[j];
                }

                if (this.lastSetVariables.Contains(variableValueArr[0]) && !tempVariableList.Contains(variableValueArr[0]))
                {
                    this.ExecuteCommand("procon.protected.send", variableValueArr[0], variableValueArr[1]);

                    if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0" + variableValueArr[0] + " " + variableValueArr[1]);

                    variableReset = true;
                }
            }

            if (!variableReset)
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^n^0No Variables Needed Reverting!");

            this.lastSetVariables = tempVariableList;
        }

        public override void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            if (this.fromRoundOver)
            {
                for (int i = 0; i < lstMaplist.Count; i++)
                {
                    if (i == this.m_currentMapIndex)
                    {
                        if (i == lstMaplist.Count - 1)
                            this.m_nextMapIndex = 0;
                        else
                            this.m_nextMapIndex = i + 1;

                        if (this.m_currentRound == 1)
                        {
                            this.m_totalRounds = lstMaplist[i].Rounds;
                            string mapMode = GetMapByFilename(lstMaplist[i].MapFileName).PublicLevelName + " - " + this.getPublicGameModeName(lstMaplist[i].Gamemode);

                            if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0Variable Settings For New Map! - (" + mapMode + ")...");

                            if (this.mode == "Map Based")
                                this.setServerVariables(mapMode);
                            else if (this.mode == "Rotation Based" && this.rotationPluginVariables.Count > 0)
                                this.setServerVariablesRotation(i);
                        }
                    }
                }

                this.m_currentMapIndex = 0;
                this.m_nextMapIndex = 0;
                this.m_currentRound = 0;
                this.m_totalRounds = 0;

                this.fromRoundOver = false;
            }
            else if (this.fromRunNext || this.fromPluginEnabled)
            {
                for (int i = 0; i < lstMaplist.Count; i++)
                {
                    if (i == this.m_currentMapIndex)
                    {
                        if (this.m_currentRound == 1)
                        {
                            string mapMode = GetMapByFilename(lstMaplist[i].MapFileName).PublicLevelName + " - " + this.getPublicGameModeName(lstMaplist[i].Gamemode);

                            if (this.displayChangesToConsole == enumBoolYesNo.Yes)
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0Variable Settings For New Map! - (" + mapMode + ")...");

                            if (this.mode == "Map Based")
                                this.setServerVariables(mapMode);
                            else if (this.mode == "Rotation Based" && this.rotationPluginVariables.Count > 0)
                                this.setServerVariablesRotation(i);
                        }
                    }
                }

                this.m_currentMapIndex = 0;
                this.m_nextMapIndex = 0;
                this.m_currentRound = 0;
                this.m_totalRounds = 0;

                this.fromRunNext = false;
                this.fromPluginEnabled = false;
            }
            else if (this.needRotation)
            {
                this.rotationPluginVariables.Clear();
                this.rotationVariables.Clear();

                for (int i = 0; i < lstMaplist.Count; i++)
                {
                    this.rotationPluginVariables.Add(GetMapByFilename(lstMaplist[i].MapFileName).PublicLevelName + " - " + this.getPublicGameModeName(lstMaplist[i].Gamemode));
                    this.rotationVariables.Add(new List<string>());
                }

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0SUCCESS! ^n- Your map rotation has been retrieved! In the ^bLoaded Plugins ^nsection to the left, click off of this plugin onto another and then back onto this one to see your rotation.");

                this.needRotation = false;
            }
        }

        public override void OnMaplistGetMapIndices(int mapIndex, int nextIndex)
        {
            if (this.fromRoundOver)
            {
                // if the current calculated round was reset to 1, that means the current map index will have to be set to the reported next map index.
                // Otherwise, the current map index will remain the same so this.m_currentMapIndex will be set to mapIndex. We have no idea what the calculated
                // next map index is going to be either way until we send mapList.list so we wait until then.
                if (this.m_currentRound == 1)
                    this.m_currentMapIndex = nextIndex;
                else
                    this.m_currentMapIndex = mapIndex;

                this.ExecuteCommand("procon.protected.send", "mapList.list");
            }
            else if (this.fromRunNext || this.fromPluginEnabled)
            {
                this.m_currentMapIndex = mapIndex;
                this.m_nextMapIndex = nextIndex;

                this.ExecuteCommand("procon.protected.send", "mapList.list");
            }
        }

        public override void OnMaplistGetRounds(int currentRound, int totalRounds)
        {
            if (this.fromRoundOver)
            {
                this.m_currentRound += currentRound + 1; // m_currentRound = m_currentRound + currentRound - to adjust based off of non-index number then add 1 since coming from Round Over

                // if the calculated current round is greater than the reported totalRounds, then that means the next map is starting so reset the current round to 1 and recalculate totalRounds
                // when mapList.list is sent. Otherwise, keep keep the calculated current round the same and put totalRounds into m_totalRounds.
                if (this.m_currentRound > totalRounds)
                    this.m_currentRound = 1;
                else
                    this.m_totalRounds = totalRounds;

                this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            }
            else if (this.fromRunNext || this.fromPluginEnabled)
            {
                this.m_currentRound += currentRound; // m_currentRound = m_currentRound + currentRound - to adjust based off of non-index number
                this.m_totalRounds = totalRounds;

                this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            }
        }

        public override void OnRoundOver(int iWinningTeamID)
        {
            this.m_currentRound++; // add 1 to current round for non-index based number

            this.fromRoundOver = true;
            this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
        }

        public override void OnRunNextLevel()
        {
            this.m_currentRound++; // add 1 to current round for non-index based number

            this.fromRunNext = true;
            this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
        }

        ///// Start of Server Variable Fetching /////

        public override void OnServerName(string serverName)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add(@"vars.serverName """ + serverName + @"""");
            }
        }

        public override void OnServerDescription(string serverDescription)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add(@"vars.serverDescription """ + serverDescription + @"""");
            }
        }

        public override void OnServerMessage(string serverMessage)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add(@"vars.serverMessage """ + serverMessage + @"""");
            }
        }

        public override void OnMaxPlayers(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.maxPlayers " + limit.ToString());
            }
        }

        public override void OnIdleTimeout(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.idleTimeout " + limit.ToString());
            }
        }

        public override void OnIdleBanRounds(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.idleBanRounds " + limit.ToString());
            }
        }

        public override void OnRoundRestartPlayerCount(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.roundRestartPlayerCount " + limit.ToString());
            }
        }

        public override void OnRoundStartPlayerCount(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.roundStartPlayerCount " + limit.ToString());
            }
        }

        public override void OnGameModeCounter(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.gameModeCounter " + limit.ToString());
            }
        }

        public override void OnFriendlyFire(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.friendlyFire " + isEnabled.ToString());
            }
        }

        public override void OnUnlockMode(string mode)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.unlockMode " + mode);
            }
        }

        public override void OnTeamKillCountForKick(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.teamKillCountForKick " + limit.ToString());
            }
        }

        public override void OnTeamKillValueIncrease(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.teamKillValueIncrease " + limit.ToString());
            }
        }

        public override void OnTeamKillValueDecreasePerSecond(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.teamKillValueDecreasePerSecond " + limit.ToString());
            }
        }

        public override void OnTeamKillValueForKick(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.teamKillValueForKick " + limit.ToString());
            }
        }

        public override void OnTeamBalance(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.autoBalance " + isEnabled.ToString());
            }
        }

        public override void OnKillCam(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.killCam " + isEnabled.ToString());
            }
        }

        public override void OnMiniMap(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.miniMap " + isEnabled.ToString());
            }
        }

        public override void On3dSpotting(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.3dSpotting " + isEnabled.ToString());
            }
        }

        public override void OnMiniMapSpotting(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.miniMapSpotting " + isEnabled.ToString());
            }
        }

        ///// Start of source code compiling error /////

        public override void OnGunMasterWeaponsPreset(int preset)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.gunMasterWeaponsPreset " + preset.ToString());
            }
        }

        public override void OnVehicleSpawnAllowed(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.vehicleSpawnAllowed " + isEnabled.ToString());
            }
        }

        public override void OnVehicleSpawnDelay(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.vehicleSpawnDelay " + limit.ToString());
            }
        }

        public override void OnBulletDamage(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.bulletDamage " + limit.ToString());
            }
        }

        public override void OnOnlySquadLeaderSpawn(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.onlySquadLeaderSpawn " + isEnabled.ToString());
            }
        }

        public override void OnSoldierHealth(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.soldierHealth " + limit.ToString());
            }
        }

        public override void OnPlayerManDownTime(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.playerManDownTime " + limit.ToString());
            }
        }

        public override void OnPlayerRespawnTime(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.playerRespawnTime " + limit.ToString());
            }
        }

        public override void OnNameTag(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.nameTag " + isEnabled.ToString());
            }
        }

        public override void OnHud(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.hud " + isEnabled.ToString());
            }
        }

        public override void OnPremiumStatus(bool isEnabled)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.premiumStatus " + isEnabled.ToString());
            }
        }

        public override void OnCtfRoundTimeModifier(int limit)
        {
            if (this.needDefaults)
            {
                this.defaultVariables.Add("vars.ctfRoundTimeModifier " + limit.ToString());
                this.needDefaults = false;
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^3^b[Map Variables] ^0Fetch complete! ^n- Input for 3 undetectable variables needed!");

                if (this.mode != "Choose a mode..." && !this.hasAllVariables())
                    this.mode = "Choose a mode...";
            }
        }

    }
}