using System;
using System.Collections.Generic;
using System.Text;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core;
using System.Threading;

namespace PRoConEvents
{
    class SquadStats : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Weapon Message class
        /// <summary>
        /// Allow any kind of weapon to be displayed in the messages.
        /// </summary>
        private class WeaponMsg
        {
            private string _Name;
            private string _Msg;
            private bool _isEOR;
            private bool _isRO;
            private bool _isAuto;
            private bool _isRndm;

            public string Name
            {
                get { return _Name; }
                set { _Name = value; }
            } // Name of weapon
            public string Msg
            {
                get { return _Msg; }
                set { _Msg = value; }
            } // Message to display
            public bool isEOR
            {
                get { return _isEOR; }
                set { _isEOR = value; }
            } // End of Round
            public bool isRO
            {
                get { return _isRO; }
                set { _isRO = value; }
            } // Round Over
            public bool isAuto
            {
                get { return _isAuto; }
                set { _isAuto = value; }
            } // Automated
            public bool isRndm
            {
                get { return _isRndm; }
                set { _isRndm = value; }
            } // Random

            public WeaponMsg(string name)
            {
                Name = name;
                Msg = "Best " + name + " Squad: %Squad% (Kills: %Kills%)";
                initTriggers();
            }

            public WeaponMsg(string name, string msg)
            {
                Name = name;
                Msg = msg;
                initTriggers();
            }

            public WeaponMsg(string name, string msg, string triggers)
            {
                Name = name;
                Msg = msg;
                setTriggers(triggers);
            }

            public void initTriggers()
            {
                isEOR = false;
                isRO = false;
                isAuto = false;
                isRndm = false;
            }

            public void setTriggers(string type)
            {
                string[] triggers = type.Split(',');
                foreach (string temp in triggers)
                {
                    string trigger = temp.Trim().ToLower();
                    if (trigger.Equals("eor")) { isEOR = true; }
                    else if (trigger.Equals("ro")) { isRO = true; }
                    else if (trigger.Equals("auto")) { isAuto = true; }
                    else if (trigger.Equals("random")) { isRndm = true; }
                }
            }
        }
        #endregion

        #region Player class
        /// <summary>
        /// Information we wish to store about each player.
        /// Extends CPlayerInfo, adds weapons stats.
        /// </summary>
        private class CPlayerInfoEx : CPlayerInfo
        {
            private ushort _KnifeKills;
            private ushort _Headshots;
            private Dictionary<string, ushort> _WeaponStats;

            public ushort KnifeKills
            {
                get { return _KnifeKills; }
                set { _KnifeKills = value; }
            }
            public ushort Headshots
            {
                get { return _Headshots; }
                set { _Headshots = value; }
            }
            public Dictionary<string, ushort> WeaponStats
            {
                get { return _WeaponStats; }
                set { _WeaponStats = value; }
            }

            public CPlayerInfoEx() : base()
            {
                Headshots = 0;
                KnifeKills = 0;
                WeaponStats = new Dictionary<string, ushort>();
            }

            public CPlayerInfoEx(CPlayerInfo player) :
                base(player.SoldierName, player.ClanTag, player.TeamID, player.SquadID) // private setters, grr
            {
                Headshots = 0;
                KnifeKills = 0;
                WeaponStats = new Dictionary<string, ushort>();
                setCPlayerInfo(player);
            }

            public CPlayerInfoEx(CPlayerInfo player, ushort oldKnifeKills, ushort oldHeadShots, Dictionary<string, ushort> oldWeaponStats) :
                base(player.SoldierName, player.ClanTag, player.TeamID, player.SquadID) // private setters, grr
            {
                Headshots = oldHeadShots;
                KnifeKills = oldKnifeKills;
                WeaponStats = oldWeaponStats;
                setCPlayerInfo(player);
            }

            public void setCPlayerInfo(CPlayerInfo player)
            {
                if (player != null)
                {
                    this.Deaths = player.Deaths;
                    this.Kdr = player.Kdr;
                    this.Kills = player.Kills;
                    this.Ping = player.Ping;
                    this.Score = player.Score;
                    this.SquadID = player.SquadID;
                    this.TeamID = player.TeamID;
                }
            }

            public void addKnifeKill()
            {
                KnifeKills++;
            }

            public void addHeadshot()
            {
                Headshots++;
            }

            public void resetStats()
            {
                KnifeKills = 0;
                Headshots = 0;
                WeaponStats = new Dictionary<string, ushort>();
            }

            public void addWeaponKill(string weaponName)
            {
                ushort value;
                if (WeaponStats.TryGetValue(weaponName, out value))
                {
                    WeaponStats[weaponName] = value++;
                }
                else
                {
                    WeaponStats.Add(weaponName, 1);
                }
            }

            public ushort getWeaponKills(string weaponName)
            {
                ushort value = 0;
                WeaponStats.TryGetValue(weaponName, out value);
                return value;
            }

            public int getObjectiveScore()
            {
                int score = 0;
                try
                {
                    score = Score - (Kills * 100); // Close enough
                }
                catch (Exception e) { }
                return score;
            }
        }
        #endregion

        #region Squad class
        /// <summary>
        /// Squad containing a list of Players.
        /// </summary>
        private class Squad
        {
            private List<CPlayerInfoEx> _SquadMembers;
            private string _Name;

            public List<CPlayerInfoEx> SquadMembers
            {
                get { return _SquadMembers; }
                set { _SquadMembers = value; }
            }
            public string Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            public Squad()
            {
                SquadMembers = new List<CPlayerInfoEx>();
            }

            public Squad(string squadName)
            {
                SquadMembers = new List<CPlayerInfoEx>();
                Name = squadName;
            }

            public void addMember(CPlayerInfoEx player)
            {
                if (player != null)
                {
                    SquadMembers.Add(player);
                }
            }

            public void updateMember(CPlayerInfo player) // Will add the player if no previous record is found.
            {
                bool updated = false;
                for (int i = 0; i < SquadMembers.Count; i++)
                {
                    if (player.SoldierName.Equals(SquadMembers[i].SoldierName))
                    {
                        SquadMembers[i] = new CPlayerInfoEx(player, SquadMembers[i].KnifeKills, SquadMembers[i].Headshots, SquadMembers[i].WeaponStats);
                        updated = true;
                    }
                }

                if (!updated)
                {
                    SquadMembers.Add(new CPlayerInfoEx(player));
                }
            }

            public void updateMemberEx(CPlayerInfoEx playerEx) // Only update
            {
                for (int i = 0; i < SquadMembers.Count; i++)
                {
                    if (playerEx.SoldierName.Equals(SquadMembers[i].SoldierName))
                    {
                        SquadMembers[i] = playerEx;
                    }
                }
            }

            public bool removeMember(CPlayerInfo player)
            {
                bool success = false;
                int removeIndex = -1;
                for (int i = 0; i < SquadMembers.Count; i++)
                {
                    if (player.SoldierName.Equals(SquadMembers[i].SoldierName))
                    {
                        removeIndex = i;
                    }
                }
                if (removeIndex >= 0)
                {
                    SquadMembers.RemoveAt(removeIndex);
                    success = true;
                }
                return success;
            }

            public CPlayerInfoEx findSquadMember(string playerName) // find player in squad matching playerName
            {
                CPlayerInfoEx result = null;
                foreach (CPlayerInfoEx player in SquadMembers)
                {
                    if (player.SoldierName.Equals(playerName))
                    {
                        result = player;
                    }
                }
                return result;
            }

            public CPlayerInfoEx getSquadLeader() // Only works when synchronized
            {
                CPlayerInfoEx player = null;
                if (SquadMembers.Count > 0)
                {
                    player = SquadMembers[0];
                }
                return player;
            }

            public int getScore()
            {
                int totalScore = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalScore += player.Score;
                    }
                }
                return totalScore;
            }

            public int getKills()
            {
                int totalKills = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalKills += player.Kills;
                    }
                }
                return totalKills;
            }

            public int getDeaths()
            {
                int totalDeaths = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalDeaths += player.Deaths;
                    }
                }
                return totalDeaths;
            }

            public float getKd()
            {
                float totalKd = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalKd += player.Kdr;
                    }
                }
                totalKd = totalKd / SquadMembers.Count;
                return totalKd;
            }

            public int getHeadshots()
            {
                int totalHeadshots = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalHeadshots += player.Headshots;
                    }
                }
                return totalHeadshots;
            }

            public int getKnifeKills()
            {
                int totalKnifeKills = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalKnifeKills += player.KnifeKills;
                    }
                }
                return totalKnifeKills;
            }

            public int getTotalWeaponStat(string weaponName)
            {
                int totalKills = 0;
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        totalKills += player.getWeaponKills(weaponName);
                    }
                }
                return totalKills;
            }

            public override string ToString()
            {
                string res = Name + " Members: ";
                if (SquadMembers.Count > 0)
                {
                    foreach (CPlayerInfoEx player in SquadMembers)
                    {
                        res += player.SoldierName + "; ";
                    }
                }
                res += "\n";
                return res;
            }

            public void resetSquadStats()
            {
                foreach (CPlayerInfoEx player in SquadMembers)
                {
                    player.resetStats();
                }
            }
        }
        #endregion

        #region Team class
        /// <summary>
        /// Team Contains 8 squads (Alpha .. Hotel)
        /// </summary>
        private class Team
        {
            private List<Squad> _Squads;
            private string _Name;

            public List<Squad> Squads
            {
                get { return _Squads; }
                set { _Squads = value; }
            }
            public string Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            public void init()
            {
                Squads = new List<Squad>();
                for (int i = 1; i <= 8; i++)
                {
                    Squads.Add(new Squad(getSquadName(i)));
                }
            }

            public Team()
            {
                Name = "Default Name";
                init();
            }

            public Team(string teamName)
            {
                Name = teamName;
                init();
            }

            public string getSquadName(int index) // used in init()
            {
                string name = "";
                switch (index)
                {
                    case 1:
                        name = "Alpha";
                        break;
                    case 2:
                        name = "Bravo";
                        break;
                    case 3:
                        name = "Charlie";
                        break;
                    case 4:
                        name = "Delta";
                        break;
                    case 5:
                        name = "Echo";
                        break;
                    case 6:
                        name = "Foxtrot";
                        break;
                    case 7:
                        name = "Golf";
                        break;
                    case 8:
                        name = "Hotel";
                        break;
                    default:
                        name = "None";
                        break;
                }
                return name;
            }

            public CPlayerInfoEx findTeamMember(string playerName) // Search every squad for playerName
            {
                CPlayerInfoEx result = null;
                CPlayerInfoEx temp = null;
                foreach (Squad s in Squads)
                {
                    temp = s.findSquadMember(playerName);
                    if (temp != null)
                    {
                        result = temp;
                    }
                }
                return result;
            }

            public bool removeTeamMember(CPlayerInfo player)
            {
                bool result = false;
                try
                {
                    result = Squads[player.SquadID - 1].removeMember(player);
                }
                catch (Exception e)
                {
                    // Failed to remove player
                }
                return result;
            }

            public void addTeamMember(CPlayerInfoEx player)
            {
                Squads[player.SquadID - 1].addMember(player);
            }

            public void resetTeamStats()
            {
                foreach (Squad s in Squads)
                {
                    s.resetSquadStats();
                }
            }

            public Squad getBestSquad() // based on score
            {
                Squad best = new Squad();
                int bs = 0; // best score
                foreach (Squad s in Squads)
                {
                    int ts = s.getScore(); // temp score
                    if (ts > bs)
                    {
                        best = s;
                        bs = ts;
                    }
                }
                return best;
            }

            public CPlayerInfoEx getBestSquadLeader() // Don't forget to check Sync otherwise might not be squadleader
            {
                CPlayerInfoEx bestSquadLeader = new CPlayerInfoEx();
                int bestScore = 0;
                foreach (Squad s in Squads)
                {
                    CPlayerInfoEx tempLeader = s.getSquadLeader(); // could be null
                    if (tempLeader != null)
                    {
                        int tempScore = tempLeader.getObjectiveScore();
                        if (tempScore > bestScore)
                        {
                            bestSquadLeader = tempLeader;
                            bestScore = tempScore;
                        }
                    }
                }
                return bestSquadLeader;
            }

            public Squad getSquadMostKills() // best based on kills
            {
                Squad best = new Squad();
                int mk = 0; // most kills
                foreach (Squad s in Squads)
                {
                    int tk = s.getKills(); // temp kills
                    if (tk > mk)
                    {
                        best = s;
                        mk = tk;
                    }
                }
                return best;
            }

            public Squad getSquadMostHeadshots()
            {
                Squad best = new Squad();
                int mh = 0; // most headshots
                foreach (Squad s in Squads)
                {
                    int th = s.getHeadshots(); // temp headshots
                    if (th > mh)
                    {
                        best = s;
                        mh = th;
                    }
                }
                return best;
            }

            public Squad getSquadMostKnifekills()
            {
                Squad best = new Squad();
                int mkk = 0; // most knife kills
                foreach (Squad s in Squads)
                {
                    int tkk = s.getKnifeKills(); // temp knife kills
                    if (tkk > mkk)
                    {
                        best = s;
                        mkk = tkk;
                    }
                }
                return best;
            }

            public Squad getSquadBestKdr()
            {
                Squad best = new Squad();
                double bkd = 0.0; // best KD
                foreach (Squad s in Squads)
                {
                    double tkd = s.getKd(); // temp KD
                    if (tkd > bkd)
                    {
                        best = s;
                        bkd = tkd;
                    }
                }
                return best;
            }

            public Squad getSquadBestWeaponStat(string weaponName) // custom weapons
            {
                Squad best = new Squad();
                int bws = 0; // Best weapon stats
                foreach (Squad s in Squads)
                {
                    int tws = s.getTotalWeaponStat(weaponName); // temp weapon stat
                    if (tws > bws)
                    {
                        best = s;
                        bws = tws;
                    }
                }
                return best;
            }
        }
        #endregion

        #region Game class
        /// <summary>
        /// A game contains 2 or 4 teams
        /// </summary>
        private class Game
        {
            #region Initialization
            private List<Team> _Teams;

            public List<Team> Teams
            {
                get { return _Teams; }
                set { _Teams = value; }
            }

            public Game()
            {
                initTeamsByAmount(2);
            }

            public Game(byte amount)
            {
                initTeamsByAmount(amount);
            }

            public Game(string gamemode)
            {
                initTeamsByGamemode(gamemode);
            }

            public void initTeamsByAmount()
            {
                byte amount = 2;
                Teams = new List<Team>();
                if (amount != 4 && amount != 2)
                {
                    amount = 2;
                }
                if (amount == 2)
                {
                    Teams.Add(new Team("US"));
                    Teams.Add(new Team("RU"));
                }
                else
                {
                    Teams.Add(new Team("Alpha"));
                    Teams.Add(new Team("Bravo"));
                    Teams.Add(new Team("Charlie"));
                    Teams.Add(new Team("Delta"));
                }
            }

            public void initTeamsByAmount(byte amount)
            {
                Teams = new List<Team>();
                if (amount != 4 && amount != 2)
                {
                    amount = 2;
                }
                if (amount == 2)
                {
                    Teams.Add(new Team("US"));
                    Teams.Add(new Team("RU"));
                }
                else
                {
                    Teams.Add(new Team("Alpha"));
                    Teams.Add(new Team("Bravo"));
                    Teams.Add(new Team("Charlie"));
                    Teams.Add(new Team("Delta"));
                }
            }

            public void initTeamsByGamemode(string gamemode)
            {
                byte amountOfTeams = 2;
                if (gamemode.Trim().ToLower().Equals("squaddeathmatch0"))
                {
                    amountOfTeams = 4;
                }
                initTeamsByAmount(amountOfTeams);
            }
            #endregion

            #region Player Management
            public CPlayerInfoEx findPlayer(string playerName)
            {
                CPlayerInfoEx player = null;
                if (Teams.Count > 0)
                {
                    int i = 0;
                    while (i < Teams.Count && player == null)
                    {
                        player = Teams[i].findTeamMember(playerName);
                        i++;
                    }
                }
                return player;
            }

            public bool doesPlayerExist(CPlayerInfo player)
            {
                bool result = false;
                CPlayerInfoEx playerResult = null;
                try
                {
                    playerResult = Teams[player.TeamID - 1].Squads[player.SquadID - 1].findSquadMember(player.SoldierName);
                }
                catch (Exception e) { }
                if (playerResult != null)
                {
                    result = true;
                }
                return result;
            }

            public void updatePlayerInfo(CPlayerInfo player) // Try to update according to known squad & team
            {
                try
                {
                    Teams[player.TeamID - 1].Squads[player.SquadID - 1].updateMember(player);
                }
                catch (Exception e) // Not found / Array index out of bounds
                {
                    // update failed
                }
            }

            public void updatePlayerInfoEx(CPlayerInfoEx playerEx) // Try to update according to known squad & team
            {
                try
                {
                    Teams[playerEx.TeamID - 1].Squads[playerEx.SquadID - 1].updateMemberEx(playerEx);
                }
                catch (Exception e) // Not found / Array index out of bounds
                {
                    // update failed
                }
            }

            public void updatePlayer(CPlayerInfo cpiPlayer)
            {
                if (cpiPlayer.SquadID > 0) // Is player in a squad?
                {
                    // Update?
                    if (doesPlayerExist(cpiPlayer)) // Do we know this player?
                    {
                        updatePlayerInfo(cpiPlayer);
                    }
                    else // New, Changed Team or Squad
                    {
                        CPlayerInfoEx tempPlayer = findPlayer(cpiPlayer.SoldierName);
                        if (tempPlayer == null) // No previous stats - New Player
                        {
                            updatePlayerInfo(cpiPlayer);
                        }
                        else // swapped team or squad
                        {
                            removePlayerInfo(cpiPlayer); // remove old stats
                            playerMoved(tempPlayer, cpiPlayer); // move player stats
                        }
                    }
                }
                else // Not in a squad - remove stats
                {
                    removePlayerInfo(cpiPlayer);
                }
            }

            public void removePlayerInfo(CPlayerInfo playerInfo)
            {
                CPlayerInfoEx playerToRemove = findPlayer(playerInfo.SoldierName);

                if (playerToRemove != null)
                {
                    Teams[playerToRemove.TeamID - 1].removeTeamMember(playerToRemove);
                }
            }

            public void playerMoved(CPlayerInfoEx oldStats, CPlayerInfo newStats)
            { // Replace old stats with new then read the player
                oldStats = new CPlayerInfoEx(newStats, oldStats.KnifeKills, oldStats.Headshots, oldStats.WeaponStats);
                Teams[oldStats.TeamID - 1].addTeamMember(oldStats);
            }
            #endregion

            #region Get best squad (Kdr, Score, Headshots,...)
            public Squad getBestSquadScore()
            {
                Squad best = new Squad();
                int bs = 0; // best score
                foreach (Team t in Teams)
                {
                    Squad temp = t.getBestSquad();
                    int ts = temp.getScore(); // temp score
                    if (ts > bs)
                    {
                        best = temp;
                        bs = ts;
                    }
                }
                return best;
            }

            public Squad getBestSquadKills()
            {
                Squad best = new Squad();
                int mk = 0; // most kills
                foreach (Team t in Teams)
                {
                    Squad temp = t.getSquadMostKills();
                    int tk = temp.getKills(); // temp kills
                    if (tk > mk)
                    {
                        best = temp;
                        mk = tk;
                    }
                }
                return best;
            }

            public Squad getBestSquadHeadshots()
            {
                Squad best = new Squad();
                int mh = 0; // most headshots
                foreach (Team t in Teams)
                {
                    Squad temp = t.getSquadMostHeadshots();
                    int th = temp.getHeadshots(); // temp headshots
                    if (th > mh)
                    {
                        best = temp;
                        mh = th;
                    }
                }
                return best;
            }

            public Squad getBestSquadKnifeKills()
            {
                Squad best = new Squad();
                int mkk = 0; // most knife kills
                foreach (Team t in Teams)
                {
                    Squad temp = t.getSquadMostKnifekills(); // temo knife kills
                    int tkk = temp.getKnifeKills();
                    if (tkk > mkk)
                    {
                        best = temp;
                        mkk = tkk;
                    }
                }
                return best;
            }

            public Squad getBestSquadKdr()
            {
                Squad best = new Squad();
                double bkd = 0.0; //  best KD
                foreach (Team t in Teams)
                {
                    Squad temp = t.getSquadBestKdr();
                    double tkd = temp.getKd(); // temp KD
                    if (tkd > bkd)
                    {
                        best = temp;
                        bkd = tkd;
                    }
                }
                return best;
            }

            public Squad getBestSquadCustomStat(string weaponName) // Pick your own weapons
            {
                Squad best = new Squad();
                int bws = 0; // best weapon stat
                foreach (Team t in Teams)
                {
                    Squad temp = t.getSquadBestWeaponStat(weaponName);
                    int tws = temp.getTotalWeaponStat(weaponName);
                    if (tws > bws)
                    {
                        best = temp;
                        bws = tws;
                    }
                }
                return best;
            }

            public CPlayerInfoEx getBestSquadleader()
            {
                CPlayerInfoEx best = new CPlayerInfoEx();
                int bs = 0; // best SL score
                foreach (Team t in Teams)
                {
                    CPlayerInfoEx temp = t.getBestSquadLeader();
                    int ts = temp.getObjectiveScore(); // temp SL score
                    if (ts > bs)
                    {
                        best = temp;
                        bs = ts;
                    }
                }
                return best;
            }
            #endregion
        }
        #endregion

        #region Sync class
        /// <summary>
        /// List for synchronization. When the plugin first starts it will save the first player list it receives.
        /// synchronization will be complete when the list only contains 1 player per squad or less. (Then we can figure out who the squadleader is)
        /// </summary>
        private class SyncList
        {
            private List<CPlayerInfo> syncPlayers;
            private bool sync;
            private int originalCount; // Just to display progress
            private double _Progress;

            public double Progress
            {
                get { return _Progress; }
                set { _Progress = value; }
            }

            public SyncList()
            {
                syncPlayers = new List<CPlayerInfo>();
                sync = true;
                originalCount = 0;
                Progress = 0.0;
            }

            public void syncRemovePlayer(CPlayerInfo player) // remove player from sync list
            {
                if (sync)
                {
                    foreach (CPlayerInfo p in syncPlayers) // search for soldier name
                    {
                        if (p.SoldierName.Equals(player.SoldierName))
                        {
                            syncPlayers.Remove(p); // remove
                        }
                    }
                }
            }

            public void removeNonePlayers() // Remove players in squad "None"
            {
                List<CPlayerInfo> playersToRemove = new List<CPlayerInfo>();
                foreach (CPlayerInfo player in syncPlayers)
                {
                    if (player.SquadID == 0)
                    {
                        playersToRemove.Add(player);
                    }
                }
                foreach (CPlayerInfo player in playersToRemove)
                {
                    syncPlayers.Remove(player);
                }
            }

            public void synchronize(List<CPlayerInfo> update)
            {
                if (update.Count == 0)
                { // Server Empty - easy
                    sync = false;
                }
                else if (sync) // Start sync
                {
                    if (syncPlayers.Count == 0) // first time - just fill list
                    {
                        syncPlayers.AddRange(update);
                        originalCount = update.Count;
                        removeNonePlayers();
                    }
                    else // update list
                    {
                        List<CPlayerInfo> playersToRemove = new List<CPlayerInfo>();
                        foreach (CPlayerInfo player in syncPlayers)
                        {
                            foreach (CPlayerInfo updatePlayer in update)
                            {
                                if (updatePlayer.SoldierName.Equals(player.SoldierName) && !updatePlayer.SquadID.Equals(player.SquadID))
                                {
                                    playersToRemove.Add(player); // Player still exists, but swapped squad
                                }
                            }
                        }
                        foreach (CPlayerInfo player in playersToRemove)
                        {
                            syncPlayers.Remove(player);
                        }
                    }
                    checkStatus();
                }
            }

            /// <summary>
            /// Make a small list of every squad and the amount of unresolved players.
            /// Unresolved means there are more than 2 in a squad so we don't know who the squadleader is.
            /// If this is the case keep synchronizing.
            /// </summary>
            public void checkStatus()
            {
                Dictionary<string, int> tempLst = new Dictionary<string, int>(); // List of all squads and amount of players per squad
                foreach (CPlayerInfo player in syncPlayers)
                {
                    string squadName = (new Team()).getSquadName(player.SquadID) + player.TeamID; // Generate squad name + team ID
                    if (!tempLst.ContainsKey(squadName)) // squad doesn't exist yet
                    {
                        tempLst.Add(squadName, 1);
                    }
                    else
                    {
                        tempLst[squadName]++;
                    }
                }
                int totalCount = 0; // Progress
                bool keepSyncing = false;
                foreach (KeyValuePair<string, int> pair in tempLst)
                {
                    totalCount += pair.Value; // progress
                    if (pair.Value > 1) // 2 or more unresolved players in a squad
                    {
                        keepSyncing = true;
                    }
                }
                sync = keepSyncing;
                if (originalCount > 0 && sync)
                {
                    Progress = Math.Round((100.0 - (((double)syncPlayers.Count / (double)originalCount) * 100)), 2);
                }
                else
                {
                    Progress = 100;
                }
            }
        }
        #endregion

        #region Compile and init events
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name,
                "OnListPlayers",
                "OnPlayerLeft",
                "OnPlayerSquadChange",
                "OnPlayerTeamChange",
                "OnPlayerKilled",
                "OnRoundOver",
                "OnLevelLoaded",
                "OnServerInfo");
        }

        public void OnPluginEnable()
        {
            init();
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSquad Stats: ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "serverInfo");
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            running = true;
            startServerInfoLoop();
            startSpam();
        }

        public void OnPluginDisable()
        {
            running = false;
            stopServerInfoLoop();
            stopSpam();
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSquad Stats: ^1Disabled.");
        }
        #endregion

        #region Plugin details
        public string GetPluginName()
        {
            return "Squad Stats";
        }

        public string GetPluginVersion()
        {
            return "v1.2";
        }

        public string GetPluginAuthor()
        {
            return "stealth";
        }

        public string GetPluginWebsite()
        {
            return "GentsGame.com";
        }

        public string GetPluginDescription()
        {
            return @"<h2>Description</h2>
                       <p>Squad Stats keeps track of stats per squad and who the squadleaders are. This allows you to give praise to squads that are performing well or squadleaders.</p>

                     <h2>Settings</h2>
					 <h3>General Settings</h3>
						<h4>How many Tickets remaining for EOR message</h4>
						<blockquote>
						<p>Set the amount of tickets left remaining to trigger the end of round message. If one of the teams drops to this amount of tickets or lower. It will display the stats in chat.</p>
						<p><b>Default:</b> 10</p>
						<p><b>Valid:</b> >5</p>
						</blockquote>
						
						<h4>How many seconds between automated messages</h4>
						<blockquote>
						<p>Set the amount of time between automated messages in seconds.</p>
						<p><b>Default:</b> 180</p>
						<p><b>Valid:</b> >1</p>
						</blockquote>
						
						<h4>Debug level</h4>
                        <blockquote>
                        <p>Adjusting this is only required for debugging purposes, otherwise just disregard.</p>
                        <p><ul>
                            <li>128 - Other Events (Round End, Level Load)</li>
                            <li>64  - Player Events (Player joined, left, swapped)</li>
                            <li>32  - Random Messages</li>
                            <li>16  - Anything that refers to printing messages ingame</li>
                            <li>8   - Initializing</li>
                            <li>4   - OnKill</li>
                            <li>2   - ListPlayers (Excessive spam)</li>
                            <li>1   - ServerInfo (Excessive spam)</li>
                            <li>0   - None</li>
                        </ul></p>
                        <p>Pick any number or a sum of numbers to to display the corresponding messages.
                        For instance to display OnKill & listPlayer messages (4 + 2) fill in 6 as the debug level.</p>
                        <p><b>Default:</b> 0</p>
                        <p><b>Valid:</b> 0 - 255</p>
                        </blockquote>

                        <h4>Config</h4>
						<blockquote>
						<p>See Below for more information.</p>
						</blockquote>
						
				      <h3>Random Stat Trigger Settings</h3>
                        <p>These settings define when the random messages are displayed.</p>
						<h4>Round Over</h4>
						<blockquote>
						<p>Should the stats be displayed when the round ends (You'll see it at the same time you see ""Your team Won/Lost"")</p>
						<p><b>Default:</b> No</p>
						</blockquote>
						
						<h4>End of round</h4>
						<blockquote>
						<p>Should the stats be displayed at the end of the round. This ties in with the ""How many Tickets remaining for EOR message"" setting.</p>
						<p><b>Default:</b> No</p>
						</blockquote>
						
						<h4>Automated</h4>
						<blockquote>
						<p>Should the stats be displayed at a timed interval. This ties in with the ""How many seconds between automated messages"" setting.</p>
						<p><b>Default:</b> No</p>
						</blockquote>
						
                      <h3>Config</h3>
                        <blockquote>
                        <p>The config setting defines which messages you would like to display and what the message is. Each line has to have the following layout:</p>
                        <p><b>[Weapon name];[Message];[trigger1],[trigger2]</b></p>
						</blockquote>
                        
                      <h4>Weapon name</h4>
                        <blockquote>
                        <p>Replace the weapon name with the name of the weapon you wish to monitor. For instance: ""hk53"".
                        It is not case sensitive.
                        If you are not sure what the name of the weapon is, change the debug level to 4 then kill somebody with the desired weapon.
                        Procon's debug messages will the contain the name of the weapon ([OnKill] Detected: ""Weapon Name"" by ""Player Name"").
                        You don't have to fill in the full weapon name, but the name does have to be unique (Just filling M4 in will monitor M4A1, M416, M40A5,... Any weapon containing M4).</p>
                        <p>The following names are reserved:</p>
                        <p><ul>
                            <li>score - Displays the squad with the highest score.</li>
                            <li>kill - Displays the squad with the highest kill count.</li>
                            <li>headshot - Displays the squad with the most headshots.</li>
                            <li>knife - Displays the squad with the most knife kills (both meelee & knife).</li>
                            <li>squadleader - Displays the best squadleader.</li>
                        </ul></p>
						</blockquote>
                        
                      <h4>Message</h4>
                        <blockquote>
                        <p>Once you have specified the weapon name you have to type in a message to display. For instance: ""Best G53 Squad: %Squad% (Kills: %Kills%)"".</p>
                        <p><ul>
                          <li>%Squad% is a placeholder for the names of the squad members in the best squad.</li>
                          <li>%Kills% is a placeholder for the total amount of kills / headshots (This won't work for score or squadleader).</li>
                          <li>%Squadleader% is a placeholder for the name of the best squadleader only works in the squadleader message.</li>
                        </ul></p>
						</blockquote>

                      <h4>Triggers</h4>
                        <blockquote>
                        <p>Lastly you have to provide a list of triggers separated by commas. For instance: ""eor, random"".</p>
                        <p><ul>
                          <li>eor - This stands for End of round. This ties in with the ""How many Tickets remaining for EOR message"" setting.</li>
                          <li>ro - This stands for Round over. The message will be displayed when the round ends (You'll see it at the same time you see ""Your team Won/Lost"").</li>
                          <li>auto - This stands for automated. The message will be displayed at a timed interval. This ties in with the ""How many seconds between automated messages"" setting.</li>
                          <li>random - Add this message to the list of random messages.</li>
                        </ul></p>
						</blockquote>
                      <p>If we put all these elements together we'd get this: ""hk53;Best G53 Squad: %Squad% (Kills: %Kills%);eor, random"". This message will be displayed at the end of a round and it will be added to the choices for the random stat. The message tracks the squad with the most G53 kills.</p>

                      <h3>Random Messages</h3>
                        <blockquote>
                        <p>If you have enabled random messages by setting any of the random stat triggers to ""true"", then read on. Random messages will check your config for lines that are specified as ""random"". These messages will be added to a pool. It will then select one of these messages at random and display it when a random message request is triggered.</p>
                        <p>It will only select messages that are not 0. For instance if the G53 stat is set to random, but nobody has made any kills with the G53 then this stat will not be chosen. If none of the random messages are eligible nothing will be displayed.</p>
						</blockquote>

				    <h2>Squadleader & Synchronization</h2>
						<p>When you first enable the plugin it will begin synchronizing. During this time it tries to figure out who the squadleaders are. The amount of time this takes varies on the activity of your server. Before synchronization is complete displaying the squadleader will not work! (No message will be displayed)</p>
						<p>The best squadleader is calculated like this: Total Score - (Amount of kills x 100). So the squadleader with the most ""objective"" score is the best squadleader.</p>";
        }
        #endregion

        #region Plugin variables
        // Config
        private static string[] defaultConfig = {
                                             "score;Best Squad (Score): %Squad%;eor,auto",
                                             "squadleader;Best SquadLeader: %Squadleader%;eor",
                                             "kill;Best Squad (Kills: %Kills%): %Squad% ;random",
                                             "headshot;Best Squad (Headshots: %Kills%): %Squad%;random",
                                             "knife;Best Squad (Knife kills: %Kills%): %Squad%;random"
                                         };
        private List<string> m_config = new List<string>(defaultConfig);
        // General
        private int m_eorTickets = 10;
        private int m_autoMsgDelay = 180;
        private byte m_debug = 0;
        // Random
        private enumBoolYesNo r_eor = enumBoolYesNo.No;
        private enumBoolYesNo r_ro = enumBoolYesNo.No;
        private enumBoolYesNo r_auto = enumBoolYesNo.No;

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            // General
            lstReturn.Add(new CPluginVariable("General|How many Tickets remaining for EOR message", typeof(int), this.m_eorTickets));
            lstReturn.Add(new CPluginVariable("General|How many seconds between automated messages", typeof(int), this.m_autoMsgDelay));
            lstReturn.Add(new CPluginVariable("General|Config", typeof(string[]), this.m_config.ToArray()));
            lstReturn.Add(new CPluginVariable("General|Debug level", typeof(int), this.m_debug));
            lstReturn.Add(new CPluginVariable("Random Stat Triggers|End of round", typeof(enumBoolYesNo), this.r_eor));
            lstReturn.Add(new CPluginVariable("Random Stat Triggers|Round over", typeof(enumBoolYesNo), this.r_ro));
            lstReturn.Add(new CPluginVariable("Random Stat Triggers|Automated", typeof(enumBoolYesNo), this.r_auto));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            // General
            lstReturn.Add(new CPluginVariable("How many Tickets remaining for EOR message", typeof(int), this.m_eorTickets));
            lstReturn.Add(new CPluginVariable("How many seconds between automated messages", typeof(int), this.m_autoMsgDelay));
            lstReturn.Add(new CPluginVariable("Config", typeof(string[]), this.m_config.ToArray()));
            lstReturn.Add(new CPluginVariable("Debug level", typeof(int), this.m_debug));
            lstReturn.Add(new CPluginVariable("End of round", typeof(enumBoolYesNo), this.r_eor));
            lstReturn.Add(new CPluginVariable("Round over", typeof(enumBoolYesNo), this.r_ro));
            lstReturn.Add(new CPluginVariable("Automated", typeof(enumBoolYesNo), this.r_auto));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            byte t_debug = 0;

            // General
            if (strVariable.CompareTo("How many Tickets remaining for EOR message") == 0 && Int32.TryParse(strValue, out this.m_eorTickets) == true)
            {
                if (this.m_eorTickets >= 5)
                {
                    this.m_eorTickets = Convert.ToInt32(strValue);
                }
                else
                {
                    this.m_eorTickets = 8;
                }
            }
            if (strVariable.CompareTo("How many seconds between automated messages") == 0 && Int32.TryParse(strValue, out this.m_autoMsgDelay) == true)
            {
                if (this.m_autoMsgDelay >= 1)
                {
                    this.m_autoMsgDelay = Convert.ToInt32(strValue);
                }
                else
                {
                    this.m_autoMsgDelay = 180;
                }
            }
            if (strVariable.CompareTo("Config") == 0)
            {
                this.m_config = checkConfig(new List<string>(CPluginVariable.DecodeStringArray(strValue)));
                startSpam();
            }
            if (strVariable.CompareTo("Debug level") == 0 && Byte.TryParse(strValue, out t_debug)) this.m_debug = t_debug;
            if (strVariable.CompareTo("End of round") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) this.r_eor = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            if (strVariable.CompareTo("Round over") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) this.r_ro = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            if (strVariable.CompareTo("Automated") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.r_auto = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                startSpam();
            }
        }

        private List<string> checkConfig(List<string> cfg)
        {
            List<string> checkedConfig = new List<string>();
            CustomWeapons = new List<WeaponMsg>();
            foreach (string s in cfg)
            {
                string[] line = s.Split(';'); // weaponName;Message;triggers
                if (line.Length == 3)
                {
                    string weaponName = line[0].Trim().ToLower();
                    string weaponMsg = line[1].Trim();
                    string triggers = cleanTriggers(line[2].ToLower()); // trigger1,trigger2,trigger3

                    if (!weaponName.Equals("") && !weaponMsg.Equals("") && !triggers.Equals(""))
                    {
                        CustomWeapons.Add(new WeaponMsg(weaponName, weaponMsg, triggers));
                        checkedConfig.Add(weaponName + ";" + weaponMsg + ";" + triggers);
                    }
                }
            }
            return checkedConfig;
        }

        private string cleanTriggers(string triggers)
        {
            string cleanTriggers = "";
            string[] temp = triggers.Split(',');
            foreach (string s in temp)
            {
                string ns = s.Trim(); // eor, ro, auto - remove spaces
                if (ns.Equals("eor") || ns.Equals("auto") || ns.Equals("random") || ns.Equals("ro"))
                {
                    cleanTriggers += ns + ",";
                }
            }
            if (cleanTriggers.Length > 1) //  Could be ""
            {
                cleanTriggers = cleanTriggers.Substring(0, cleanTriggers.Length - 1); // remove last ","
            }
            return cleanTriggers;
        }
        #endregion

        #region Initialization
        private Game game;
        private SyncList syncList;
        private bool runServerInfo;
        private bool eorMsg; // End of round message
        private bool running;
        private string gamemode;
        private List<WeaponMsg> CustomWeapons; // Not really just weapons (score, kills, squadleader,...)

        private void init()
        {
            running = false;
            runSpam = false;
            syncList = new SyncList();
            eorMsg = false;
            gamemode = "";
            game = new Game();
            checkConfig(m_config);
        }

        public SquadStats()
        {
            init();
        }
        #endregion

        #region Event handler helper functions
        #region [Event] ServerInfo
        private void startServerInfoLoop()
        {
            runServerInfo = true;
            Thread serverInfoQuery = new Thread(new ThreadStart(delegate ()
            {
                while (runServerInfo)
                {
                    debugMsg("[startServerInfoLoop] serverInfo request sent", debugFlag.ServerInfo);
                    Thread.Sleep(5000);
                    this.ExecuteCommand("procon.protected.send", "serverInfo");
                }
            }));
            serverInfoQuery.Start();
        }

        private void stopServerInfoLoop()
        {
            runServerInfo = false;
        }

        private void displayLowTicketMsg(int lowTickets)
        {
            if (lowTickets <= m_eorTickets && !eorMsg) // Less or equal to 8 tickets left and message hasn't been displayed yet
            {
                eorMsg = true;
                generateStats("eor");
                generateRandomStat(r_eor);
            }
        }
        #endregion

        #region [Event] OnEndRound
        private void resetTeams()
        {
            foreach (Team t in game.Teams)
            {
                t.resetTeamStats();
            }
            eorMsg = false;
        }
        #endregion
        #endregion

        #region Auto Messages
        private bool runSpam;

        private void startSpam()
        {
            debugMsg("[Spammer] Start attempt, runSpam = " + runSpam + ", running = " + running, debugFlag.Initialization);
            if (!runSpam)
            {
                runSpam = true;
                Thread spammer = new Thread(new ThreadStart(delegate ()
                {
                    while (runSpam && running)
                    {
                        debugMsg("[Spammer] Message Attempt", debugFlag.IngameMessages);
                        runSpam = generateStats("auto");
                        if (runSpam)
                        {
                            generateRandomStat(r_auto);
                        }
                        else
                        {
                            runSpam = generateRandomStat(r_auto);
                        }
                        Thread.Sleep(m_autoMsgDelay * 1000);
                    }
                    runSpam = false;
                }));
                spammer.Start();
            }
        }

        private void stopSpam()
        {
            runSpam = false;
        }
        #endregion

        #region Send Messages
        #region basic debug / send Message
        [Flags]
        private enum debugFlag : byte // 0-255
        {
            OtherEvents = 128, // Round End, Level Load
            PlayerEvents = 64, // Player join, leave, swap
            RandomMessages = 32,
            IngameMessages = 16, // Anything that tries to print ingame
            Initialization = 8, // Stuff initializing
            OnKill = 4, // OnKill Messages
            ListPlayers = 2,
            ServerInfo = 1,
            None = 0
        }

        private void debugMsg(string msg, debugFlag flag)
        {
            if (msg != null && !msg.Equals("") && ((debugFlag)m_debug & flag) != 0)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
            }
        }

        private void sayMsg(string msg)
        {
            if (msg != null && !msg.Equals(""))
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", msg, "all");
                debugMsg("[sayMsg] Sending Message: " + msg, debugFlag.IngameMessages);
            }
        }
        #endregion

        #region Random Messages - Sloppy
        private bool generateRandomStat(enumBoolYesNo trigger) // Sloppy
        {
            bool ok = false;
            if (trigger.Equals(enumBoolYesNo.Yes))
            {
                List<WeaponMsg> randomCandidates = getRandomCandidates();
                Random generator = new Random(); // random number generator

                bool valid = false; // valid candidate (kills > 0)
                Squad squad = new Squad(); // Best squad
                CPlayerInfoEx squadleader = new CPlayerInfoEx(); // Best SL

                int index = 0; // random index
                while (!valid && randomCandidates.Count > 0)
                {
                    index = generator.Next(0, randomCandidates.Count); // pick random index
                    if (randomCandidates[index].Name.Equals("squadleader") && syncList.Progress == 100.0) // check if stat is squadleader
                    {
                        squadleader = game.getBestSquadleader();
                        if (squadleader.SoldierName != null && !squadleader.SoldierName.Equals("")) // valid SL?
                        {
                            valid = true;
                        }
                    }
                    else
                    {
                        squad = getBestSquadRandomStats(randomCandidates[index].Name); // get best squad for this stat
                        int amount = getSquadRandomStats(squad, randomCandidates[index].Name);
                        debugMsg("[generateRandomStat] valid? stat: " + randomCandidates[index].Name + " amount: " + amount, debugFlag.RandomMessages);
                        if (amount > 0) // valid?
                        {
                            valid = true;
                        }
                    }
                    debugMsg("[generateRandomStat] " + randomCandidates[index].Name + " i: " + index + " valid: " + valid, debugFlag.RandomMessages);
                    if (!valid) // Not valid SL or Stat
                    {
                        randomCandidates.RemoveAt(index); // 1 less to check
                    }
                }
                if (valid) // Might not have found any
                {
                    generateStatsLine(randomCandidates[index]);
                    ok = true; // printed message - ok!
                }
                else if (randomCandidates.Count == 0)
                {
                    ok = true; // Haven't printed message - no eligible cadidates, but keep asking
                }
            }
            return ok;
        }

        private int getSquadRandomStats(Squad squad, string type) // check amount of kills / score / etc. Needed for valid check
        {
            int amount = 0;
            if (type.Equals("score"))
            {
                amount = squad.getScore();
            }
            else if (type.Equals("kill"))
            {
                amount = squad.getKills();
            }
            else if (type.Equals("headshot"))
            {
                amount = squad.getHeadshots();
            }
            else if (type.Equals("knife"))
            {
                amount = squad.getKnifeKills();
            }
            else
            {
                amount = squad.getTotalWeaponStat(type);
            }
            return amount;
        }

        private Squad getBestSquadRandomStats(string candidate) // doesn't include squadleader
        {
            Squad squad = new Squad();
            if (candidate.Equals("score"))
            {
                squad = game.getBestSquadScore();
            }
            else if (candidate.Equals("kill"))
            {
                squad = game.getBestSquadKills();
            }
            else if (candidate.Equals("headshot"))
            {
                squad = game.getBestSquadHeadshots();
            }
            else if (candidate.Equals("knife"))
            {
                squad = game.getBestSquadKnifeKills();
            }
            else
            {
                squad = game.getBestSquadCustomStat(candidate); // get best squad for this weapon
            }
            return squad;
        }

        private List<WeaponMsg> getRandomCandidates() // get Message pool to choose from
        {
            List<WeaponMsg> randomCandidates = new List<WeaponMsg>();
            foreach (WeaponMsg w in CustomWeapons)
            {
                if (w.isRndm)
                {
                    debugMsg("[getRandomCandidates] Added: " + w.Name, debugFlag.RandomMessages);
                    randomCandidates.Add(w);
                }
            }
            return randomCandidates;
        }
        #endregion

        #region Generate Stat Messages
        private bool generateStats(string trigger)
        {
            bool ok = false; // did we print something -  needed for spammer
            foreach (WeaponMsg w in CustomWeapons)
            {
                if (trigger.Equals("eor") && w.isEOR)
                {
                    generateStatsLine(w); // generate line for EOR
                }
                else if (trigger.Equals("ro") && w.isRO)
                {
                    generateStatsLine(w); // generate line for RO
                }
                else if (trigger.Equals("auto") && w.isAuto)
                {
                    generateStatsLine(w); // generate line for Spammer
                    ok = true; // keep spammer looping
                }
            }
            return ok;
        }

        private void generateStatsLine(WeaponMsg w)
        {
            bool reserved = false;
            reserved = checkReservedMsg(w);
            if (!reserved) // custom weapon
            {
                generateSquadMsg(game.getBestSquadCustomStat(w.Name), w.Msg, w.Name);
            }
        }

        private bool checkReservedMsg(WeaponMsg weaponMessage)
        {
            bool ok = false;
            if (weaponMessage.Name.Equals("score"))
            {
                generateSquadMsg(game.getBestSquadScore(), weaponMessage.Msg);
                ok = true;
            }
            else if (weaponMessage.Name.Equals("kill"))
            {
                generateSquadMsg(game.getBestSquadKills(), weaponMessage.Msg, "kills");
                ok = true;
            }
            else if (weaponMessage.Name.Equals("headshot"))
            {
                generateSquadMsg(game.getBestSquadHeadshots(), weaponMessage.Msg, "headshots");
                ok = true;
            }
            else if (weaponMessage.Name.Equals("knife"))
            {
                generateSquadMsg(game.getBestSquadKnifeKills(), weaponMessage.Msg, "knife");
                ok = true;
            }
            else if (weaponMessage.Name.Equals("squadleader"))
            {
                generateSquadleaderMsg(weaponMessage.Msg);
                ok = true;
            }
            return ok;
        }

        private void generateSquadMsg(Squad squad, string defaultMsg)
        {
            debugMsg("[generateSquadMsg] Type 1 Generating Message default: " + defaultMsg, debugFlag.IngameMessages);
            string msg = "";
            foreach (CPlayerInfoEx player in squad.SquadMembers)
            {
                msg += player.SoldierName + ", ";
            }
            if (msg.Length > 2) //  Could be ""
            {
                msg = msg.Substring(0, msg.Length - 2); // remove last ", "
            }
            msg = defaultMsg.Replace("%Squad%", !msg.Equals("") ? msg : "Nobody");
            sayMsg(msg);
        }

        private void generateSquadMsg(Squad squad, string defaultMsg, string weapon)
        {
            debugMsg("[generateSquadMsg] Type 2 Generating Message default: " + defaultMsg + " weapon: " + weapon, debugFlag.IngameMessages);
            string msg = "";
            foreach (CPlayerInfoEx player in squad.SquadMembers)
            {
                msg += player.SoldierName + ", ";
            }
            if (msg.Length > 2) //  Could be ""
            {
                msg = msg.Substring(0, msg.Length - 2); // remove last ", "
            }
            msg = defaultMsg.Replace("%Squad%", !msg.Equals("") ? msg : "Nobody");
            if (weapon.Equals("knife"))
            {
                msg = msg.Replace("%Kills%", squad.getKnifeKills().ToString());
            }
            else if (weapon.Equals("kills"))
            {
                msg = msg.Replace("%Kills%", squad.getKills().ToString());
            }
            else if (weapon.Equals("headshots"))
            {
                msg = msg.Replace("%Kills%", squad.getHeadshots().ToString());
            }
            else
            {
                msg = msg.Replace("%Kills%", squad.getTotalWeaponStat(weapon).ToString());
            }
            sayMsg(msg);
        }

        private void generateSquadleaderMsg(string defaultMsg)
        {
            debugMsg("[generateSquadleaderMsg] Checking Sync", debugFlag.IngameMessages);
            if (syncList.Progress == 100.0) // Only if sync'd
            {
                debugMsg("[generateSquadleaderMsg] Generating Message", debugFlag.IngameMessages);
                CPlayerInfoEx best = game.getBestSquadleader();
                string msg = "";
                if (!best.SoldierName.Equals("") && best.SoldierName != null) // If there are no players online...
                {
                    msg = defaultMsg.Replace("%Squadleader%", best.SoldierName);
                }
                sayMsg(msg);
            }
        }
        #endregion
        #endregion

        #region Event Handlers
        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All && !gamemode.Equals(""))
            {
                syncList.synchronize(lstPlayers); // synchronize
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    game.updatePlayer(cpiPlayer);
                }

                // OUTPUT
                string output = "";
                foreach (Team t in game.Teams)
                {
                    output += "Team: " + t.Name + "\n";
                    foreach (Squad s in t.Squads)
                    {
                        output += s.ToString();
                    }
                }
                output += "[Synchronization] Progress: " + syncList.Progress.ToString() + "%\n";

                debugMsg(output, debugFlag.ListPlayers);
            }
        }

        public void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            debugMsg("[Event: Player Left] " + playerInfo.SoldierName, debugFlag.PlayerEvents);
            game.removePlayerInfo(playerInfo);
            syncList.syncRemovePlayer(playerInfo);
        }

        public void OnPlayerSquadChange(string soldierName, int teamId, int squadId)
        {
            debugMsg("[Event: Squad Change] " + soldierName, debugFlag.PlayerEvents);
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {
            debugMsg("[Event: Team Change] " + soldierName, debugFlag.PlayerEvents);
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPlayerKilled(Kill killDetails)
        {
            if (killDetails.Killer.SquadID > 0 && !gamemode.Equals("")) // In squad
            {
                bool update = false;
                CPlayerInfoEx killerEx = game.findPlayer(killDetails.Killer.SoldierName); // get extended info
                if (killDetails.Headshot) // headshots +1
                {
                    killerEx.addHeadshot();
                    update = true;
                    debugMsg("[OnKill] " + killerEx.SoldierName + " Headshot++", debugFlag.OnKill);
                }
                string tempDmgType = killDetails.DamageType.ToLower();
                debugMsg("[OnKill] Detected: " + tempDmgType + " by " + killerEx.SoldierName, debugFlag.OnKill);
                if (tempDmgType.Contains("knife") || tempDmgType.Contains("melee")) // knife +1
                {
                    killerEx.addKnifeKill();
                    update = true;
                    debugMsg("[OnKill] " + killerEx.SoldierName + " Knife++", debugFlag.OnKill);
                }
                else // Any other type of weapon ++
                {
                    bool foundWeapon = false;
                    int i = 0;
                    while (!foundWeapon && i < CustomWeapons.Count) // Check list - do we need to keep this stat?
                    {
                        if (tempDmgType.Contains(CustomWeapons[i].Name))
                        {
                            killerEx.addWeaponKill(CustomWeapons[i].Name);
                            update = true;
                            debugMsg("[OnKill] " + killerEx.SoldierName + " " + CustomWeapons[i].Name + "++", debugFlag.OnKill);
                            foundWeapon = true;
                        }
                        i++;
                    }
                }

                if (update)
                {
                    game.updatePlayerInfoEx(killerEx);
                }
            }
        }

        public void OnRoundOver(int iWinningTeamID) // Send messages at end of round
        {
            debugMsg("[Event: Round Over] Message Attempt", debugFlag.IngameMessages);
            generateStats("ro");
            generateRandomStat(r_ro);
            debugMsg("[Event: Round Over] Resetting stats", debugFlag.OtherEvents);
            resetTeams(); // reset Stats
        }

        public void OnServerInfo(CServerInfo serverInfo) // What is the current score?
        {
            if (gamemode.Equals("")) // Plugin freshly enabled, initialize game
            {
                gamemode = serverInfo.GameMode.Trim().ToLower();
                game = new Game(gamemode);
            }
            List<TeamScore> teamScores = serverInfo.TeamScores;
            string output = "[Event: ServerInfo] GameMode: " + serverInfo.GameMode;
            int lowestScore = 9999;
            foreach (TeamScore s in teamScores)
            {
                output += "\nTeamID: " + s.TeamID + " Score: " + s.Score + " WinningScore : " + s.WinningScore + ";";
                if (s.Score < lowestScore)
                {
                    lowestScore = s.Score;
                }
            }
            debugMsg(output, debugFlag.ServerInfo);
            displayLowTicketMsg(lowestScore);
        }

        public void OnLevelLoaded(string mapFileName, string newGamemode, int roundsPlayed, int roundsTotal) // BF3
        { // check GameMode
            newGamemode = newGamemode.Trim().ToLower();
            if (!gamemode.Equals(newGamemode) && (gamemode.Equals("squaddeathmatch0") || newGamemode.Equals("squaddeathmatch0")))
            { // Gamemodes are not the same and either the old one or new one is squad deathmatch
                debugMsg("[OnLevelLoaded] Changing teams, old Mode: " + gamemode + " new Mode: " + newGamemode, debugFlag.OtherEvents);
                game.initTeamsByGamemode(newGamemode);
            }
            gamemode = newGamemode; // old = new
        }
        #endregion
    }
}