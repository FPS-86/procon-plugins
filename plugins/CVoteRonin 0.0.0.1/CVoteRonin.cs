using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Mail;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CVoteRonin : PRoConPluginAPI, IPRoConPluginInterface
    {
        private int voteDuration;
        private int voteProgressNumber;
        private string sendEmailAddress;
        private string sendEmailAddressPassword;
        private string receiveEmailAddress;

        private CPrivileges currentPrivileges;
        private string currentVote;
        private List<string> currentVoteOptions;
        private bool bVoteInProgress;
        private List<string> alreadyVoted;
        private List<int> votes;
        private bool bFirstDisplay;
        private bool bFirstResultDisplay;
        private bool bAwaitingResultDisplay;
        private List<string> voteOptionWinners;
        private string emailBody;

        private System.Timers.Timer voteInProgress;
        private System.Timers.Timer voteProgressDisplay;

        public CVoteRonin()
        {
            this.voteDuration = 2;
            this.voteProgressNumber = 15;
            this.sendEmailAddress = "";
            this.sendEmailAddressPassword = "";
            this.receiveEmailAddress = "";

            this.currentVote = "";
            this.currentVoteOptions = new List<string>();
            this.bVoteInProgress = false;
            this.alreadyVoted = new List<string>();
            this.votes = new List<int>();
            this.bFirstDisplay = true;
            this.bFirstResultDisplay = true;
            this.bAwaitingResultDisplay = true;
            this.voteOptionWinners = new List<string>();
            this.emailBody = "";
        }

        public string GetPluginName()
        {
            return "Vote Ronin";
        }

        public string GetPluginVersion()
        {
            return "0.0.0.1";
        }

        public string GetPluginAuthor()
        {
            return "TimSad";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/showthread.php?4387-Vote";
        }

        public string GetPluginDescription()
        {
            return @"
        <h2>Description</h2>
          <p>This is a generic Voting/Poll plugin for Ronin...</p>
        <h2>Usage</h2>
          <p>In chat, type <i><b>!poll ""your poll here"" ""option one"" ""option two"" ""option three""</b></i> and so forth... You can create up to 10 options! Make sure all the options and the poll are in quotes!</p>
        ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnGlobalChat");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bVote Ronin ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bVote Ronin ^1Disabled =(");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Poll Settings|Poll Duration (in minutes)", this.voteDuration.GetType(), this.voteDuration));
            lstReturn.Add(new CPluginVariable("Poll Settings|Poll Options Display Interval (in seconds)", this.voteProgressNumber.GetType(), this.voteProgressNumber));
            lstReturn.Add(new CPluginVariable("Email|Send Email Address", this.sendEmailAddress.GetType(), this.sendEmailAddress));
            lstReturn.Add(new CPluginVariable("Email|Send Email Address Password", this.sendEmailAddressPassword.GetType(), this.sendEmailAddressPassword));
            lstReturn.Add(new CPluginVariable("Email|Receive Email Address", this.receiveEmailAddress.GetType(), this.receiveEmailAddress));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Poll Duration (in minutes)", this.voteDuration.GetType(), this.voteDuration));
            lstReturn.Add(new CPluginVariable("Poll Options Display Interval (in seconds)", this.voteProgressNumber.GetType(), this.voteProgressNumber));
            lstReturn.Add(new CPluginVariable("Send Email Address", this.sendEmailAddress.GetType(), this.sendEmailAddress));
            lstReturn.Add(new CPluginVariable("Send Email Address Password", this.sendEmailAddressPassword.GetType(), this.sendEmailAddressPassword));
            lstReturn.Add(new CPluginVariable("Receive Email Address", this.receiveEmailAddress.GetType(), this.receiveEmailAddress));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Poll Duration (in minutes)") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.voteDuration = valueAsInt;
            }
            else if (strVariable.CompareTo("Poll Options Display Interval (in seconds)") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.voteProgressNumber = valueAsInt;
            }
            else if (strVariable.CompareTo("Send Email Address") == 0)
            {
                this.sendEmailAddress = strValue;
            }
            else if (strVariable.CompareTo("Send Email Address Password") == 0)
            {
                this.sendEmailAddressPassword = strValue;
            }
            else if (strVariable.CompareTo("Receive Email Address") == 0)
            {
                this.receiveEmailAddress = strValue;
            }
        }

        private bool canStartVote(string speaker)
        {
            bool canStartVote = false;
            currentPrivileges = GetAccountPrivileges(speaker);

            if (currentPrivileges != null)
            {
                if (currentPrivileges.CanLogin)
                    canStartVote = true;
            }

            return canStartVote;
        }

        private void sendEmail()
        {
            string SendersAddress = this.sendEmailAddress;
            string ReceiversAddress = this.receiveEmailAddress;
            string SendersPassword = this.sendEmailAddressPassword;
            string subject = "Results of your poll \"" + this.currentVote + "\"";
            string body = this.emailBody;

            try
            {
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Credentials = new NetworkCredential(SendersAddress, SendersPassword);
                smtp.Timeout = 3000;

                MailMessage message = new MailMessage(SendersAddress, ReceiversAddress, subject, body);
                smtp.Send(message);
                this.ExecuteCommand("procon.protected.pluginconsole.write", "Poll results sent to your email successfully!");
            }
            catch (Exception ex)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", ex.Message);
            }
        }

        private void OnVoteInProgressEnd(object source, ElapsedEventArgs e)
        {
            this.bVoteInProgress = false;
            this.voteProgressDisplay.Enabled = false;

            this.ExecuteCommand("procon.protected.send", "admin.say", "The Poll has ended! Here are the Results...", "all");
            this.ExecuteCommand("procon.protected.tasks.add", "CPollResultsDisplay", "0", "5", "2", "procon.protected.plugins.call", "CVoteRonin", "displayPollResults");

            do
            {
                if (!this.bAwaitingResultDisplay)
                {
                    int highestVotes = 0;
                    for (int i = 0; i < this.votes.Count; i++)
                    {
                        if (this.votes[i] > highestVotes)
                            highestVotes = this.votes[i];
                    }

                    for (int i = 0; i < this.votes.Count; i++)
                    {
                        if (highestVotes == this.votes[i])
                            this.voteOptionWinners.Add(this.currentVoteOptions[i]);
                    }

                    if (this.voteOptionWinners.Count > 1)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "There was a tie between the following with " + highestVotes.ToString() + " votes each...", "all");
                        string winners = "";
                        for (int i = 0; i < this.voteOptionWinners.Count; i++)
                        {
                            if (i == this.voteOptionWinners.Count - 1)
                                winners += "and " + this.voteOptionWinners[i];
                            else
                                winners += this.voteOptionWinners[i] + ", ";
                        }

                        this.ExecuteCommand("procon.protected.send", "admin.say", winners, "all");

                        this.emailBody += "There was a tie between the following with " + highestVotes.ToString() + " votes each...\n";
                        this.emailBody += winners;
                    }
                    else if (this.voteOptionWinners.Count == 1)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "The winner was " + this.voteOptionWinners[0] + " with " + highestVotes.ToString() + " votes!", "all");
                        this.emailBody += "The winner was " + this.voteOptionWinners[0] + " with " + highestVotes.ToString() + " votes!";
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "There was no winner because no votes were cast!", "all");
                        this.emailBody += "There was no winner because no votes were cast!";
                    }

                    sendEmail();

                    this.currentVote = "";
                    this.currentVoteOptions.Clear();
                    this.bFirstDisplay = true;
                    this.bFirstResultDisplay = true;
                    this.bAwaitingResultDisplay = true;
                    this.alreadyVoted.Clear();
                    this.votes.Clear();
                    this.voteOptionWinners.Clear();
                    this.voteInProgress.Enabled = false;
                    this.emailBody = "";

                    break;
                }
            } while (true);
        }

        private void OnVoteProgressDisplay(object source, ElapsedEventArgs e)
        {
            this.bFirstDisplay = true;
            this.ExecuteCommand("procon.protected.send", "admin.say", "---------- Vote Options! Type the number in chat! ----------", "all");
            this.ExecuteCommand("procon.protected.tasks.add", "CPollOptionsDisplay", "0", "5", "2", "procon.protected.plugins.call", "CVoteRonin", "displayPollOptions");
        }

        public void displayPollOptions()
        {
            if (this.currentVoteOptions.Count >= 4)
            {
                if (this.bFirstDisplay)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", i.ToString() + " - " + currentVoteOptions[i], "all");
                    }
                }
                else
                {
                    for (int i = 4; i < this.currentVoteOptions.Count; i++)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", i.ToString() + " - " + currentVoteOptions[i], "all");
                    }
                }
            }
            else
            {
                if (this.bFirstDisplay)
                {
                    for (int i = 0; i < this.currentVoteOptions.Count; i++)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", i.ToString() + " - " + currentVoteOptions[i], "all");
                    }
                }
            }

            this.bFirstDisplay = false;
        }

        public void displayPollResults()
        {
            if (this.currentVoteOptions.Count >= 4)
            {
                if (this.bFirstResultDisplay)
                {
                    for (int i = 0; i < this.votes.Count; i++)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Votes for option #" + i.ToString() + ": " + this.votes[i].ToString(), "all");
                        this.emailBody += "Votes for \"" + this.currentVoteOptions[i] + "\" - " + this.votes[i].ToString() + "\n";
                    }
                }
                else
                {
                    for (int i = 4; i < this.votes.Count; i++)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Votes for option #" + i.ToString() + ": " + this.votes[i].ToString(), "all");
                        this.emailBody += "Votes for \"" + this.currentVoteOptions[i] + "\" - " + this.votes[i].ToString() + "\n";
                    }
                }
            }
            else
            {
                if (this.bFirstResultDisplay)
                {
                    for (int i = 0; i < this.votes.Count; i++)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Votes for option #" + i.ToString() + ": " + this.votes[i].ToString(), "all");
                        this.emailBody += "Votes for \"" + this.currentVoteOptions[i] + "\" - " + this.votes[i].ToString() + "\n";
                    }
                }
            }

            if (!this.bFirstResultDisplay)
                this.bAwaitingResultDisplay = false;

            this.bFirstResultDisplay = false;
        }

        private bool getVoteOptions(string message)
        {
            bool hasEnoughParameters = false;
            bool openQuoteFound = false;
            int startIndex = 0;
            int endIndex = 0;
            int paramNum = 0;

            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == '"')
                {
                    if (!openQuoteFound)
                    {
                        openQuoteFound = true;
                        startIndex = i + 1;
                        hasEnoughParameters = false;
                    }
                    else if (openQuoteFound)
                    {
                        openQuoteFound = false;
                        endIndex = i;
                        paramNum++;

                        if (paramNum == 1)
                            this.currentVote = message.Substring(startIndex, endIndex - startIndex);
                        else if (paramNum > 1)
                        {
                            this.currentVoteOptions.Add(message.Substring(startIndex, (endIndex - startIndex)));
                            this.votes.Add(0);
                            hasEnoughParameters = true;
                        }
                    }
                }
            }

            return hasEnoughParameters;
        }

        public override void OnGlobalChat(string speaker, string message)
        {
            if (message.StartsWith("!poll"))
            {
                if (canStartVote(speaker))
                {
                    if (getVoteOptions(message))
                    {
                        this.bVoteInProgress = true;

                        this.ExecuteCommand("procon.protected.send", "admin.yell", speaker + " put up a Poll! (Options in chat) - " + currentVote, (this.voteDuration * 60).ToString());
                        this.ExecuteCommand("procon.protected.send", "admin.say", "---------- Vote Options! Type the number in chat! ----------", "all");
                        this.ExecuteCommand("procon.protected.tasks.add", "CPollOptionsDisplay", "0", "5", "2", "procon.protected.plugins.call", "CVoteRonin", "displayPollOptions");

                        this.voteInProgress = new System.Timers.Timer((this.voteDuration * 60) * 1000);
                        this.voteInProgress.Enabled = true;
                        this.voteInProgress.Elapsed += new ElapsedEventHandler(OnVoteInProgressEnd);

                        this.voteProgressDisplay = new System.Timers.Timer(this.voteProgressNumber * 1000);
                        this.voteProgressDisplay.Enabled = true;
                        this.voteProgressDisplay.Elapsed += new ElapsedEventHandler(OnVoteProgressDisplay);
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", speaker + " - You did not supply enough arguments or are missing a closing quote.", "all");
                        this.currentVote = "";
                        this.currentVoteOptions.Clear();
                        this.votes.Clear();
                    }
                }
                else
                    this.ExecuteCommand("procon.protected.send", "admin.say", speaker + " cannot start votes!", "all");
            }

            if (this.bVoteInProgress)
            {
                for (int i = 0; i < this.currentVoteOptions.Count; i++)
                {
                    if (message.StartsWith(i.ToString()) && speaker != "Server")
                    {
                        if (!this.alreadyVoted.Contains(speaker))
                        {
                            this.votes[i]++;
                            this.alreadyVoted.Add(speaker);
                            this.ExecuteCommand("procon.protected.send", "admin.say", speaker + " - You voted for option #" + i.ToString(), "player", speaker);
                        }
                        else
                            this.ExecuteCommand("procon.protected.send", "admin.say", speaker + " - You have ALREADY voted!", "player", speaker);
                    }
                }
            }
        }

    }
}