CKillStreaks.cs
Written by: [LAB]HeliMagnet and QuarterEvil
For use with: ProCon

When running this plugin, it is imperative that you follow the convention of:
Kill Number
Message appending player's name

For example, in the plugin settings:

[0] 5
[1] "is on a five kill streak!"
[2] 8
[3] "is kicking @$$!"
[4] 15
[5] "is on fire!"

So, when a player is on a 5 kill streak, the message is: <Player Name> is on a five kill streak! 
For 15 kills: <Player Name> is on fire!

For the message entries, there is no need for a space at the beginning of the line.


You can also pick which kill count is needed to show the end kill streak message: <Player 1> has ended <Player 2>'s kill streak!

Now includes the ability to add a custom kill streak end message. 
Use %pk% for the killer (who ended the kill streak), 
    %pv% for the victim (the one who had a kill streak going), and 
    %nk% if you want the kill streak value (number).
An example: %pk% has ended %pv%'s %nk% kill streak! Which could mean: HeliMagnet has ended Phogue's 8 kill streak!


Fixed problem with team kill or suicides affecting kill streaks and kill streak messages.

Added first blood kill message for the beginning of the round. Use %pk% and %pv% to displaying the killer and victim's name in message, respectively.

Added death streak message capability (just like the kill streaks).
