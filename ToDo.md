### **Moderation Features**

**Status Key**
- ❌ Not Started
- 🔧 In Progress (maybe seperate progres keys for seperate devs)
- ✅ Completed

1. ❌**Ban/Kick/Mute System**
   - Commands:
     - ❌`/ban [user] <reason> <referenceImage> <referenceMessage>`
     - ❌`/unban [user] <reason>`
     - ❌`/mute [user] <reason> <referenceImage> <referenceMessage>`
     - ❌`/unmute [user] <reason>`
     - ❌`/timeout [user] [duration] <reason> <referenceImage> <referenceMessage>`
     - ❌`/kick [user] <reason>`
   - Include options for providing reasons and optional reference (screenshot and/or message link).
   - Display information on how to appeal actions.
   
2. ❌**Warn System**
   - Commands:
     - ❌`/warn [user] [reason] <severity:0~3 = 1> <notifyUser = true>`
     - ❌`/dewarn [user] <reason> <severity:0~3 = 1> <notifyUser = true>`
   - Severity weighting system (0-3) to track and escalate warnings.
   - Notify staff if a user surpasses a certain threshold of weighted points.

3. ❌**Purge Messages**
   - Commands:
     - ✅`/purge [number of messages]`
     - ❌`/purge until [message link, inclusive]`
    - [Documentation](Documentation/Commands/purge.md)

4. ❌**Lock System**
   - Commands:
     - ❌`/lock [channel] [time]`
     - ❌`/lock all (all channels)`
     - ❌`/unlock [channel]`
     - ❌`/unlock all (all channels)`
   - It should additionally be possible to force manual verification of users in times when raids are likely.

5. ❌**Mass Actions & Alias System**
   - Create aliases for executing multiple commands at once.
   - Commands:
     - ❌`/alias add`
     - ❌`/alias remove`
     - ❌`/alias list`
     - ❌`/run [alias name:string]`

6. ❌**Repeat Previous Command**
   - Command:
     - ❌`/rp [users as string]`
   - Useful for bulk banning or other repetitive actions.

### **Logs & Database**

7. ❌**Moderation/Member/Message Logs**
   - Configure logs for commands, moderation actions, message edits/deletions, and member joins/leaves.
   - Command:
     - ❌`/config logs [logType] [enabled:true|false]`
   - Configure log channels:
     - ❌`/config logs [logType] [channel:id|url]`
   - Example log types: `'commands'`, `'mod'`, `'message'`, `'member'`.

8. ❌**Automated Logs**
   - Implement an automated logging system to store logs for future reference.
   - Allow staff to search logs for specific users.
   - Possible feature: Alert when a new account (e.g., younger than a week/month) joins the server.

9. ❌**Chain of Custody & Escalation System**
   - Establish a system to track and escalate moderation actions.
   - Include features to manage appeals.

10. ❌**Anti-Spam/Anti-Link/Anti-Raid/Anti-Nuke**
    - Develop protections against spam, links, raids, and server nukes.

### ❌**Alias and Rolegroup Configuration**

11. ❌**Command Configuration**
    - Set command prefixes:
      - ❌`/config commands prefixes [prefixes:string[]]`
    - Enable/disable commands:
      - ❌`/config commands enabled [command:string]`
    - Set required roles for commands:
      - ❌`/config commands reqroles [command:string] [rolegroups:string]`

12. ❌**Rolegroup Management**
    - Create, remove, and manage rolegroups:
      - ❌`/config rolegroup create [name:string] <roles:string>`
      - ❌`/config rolegroup remove [name:string]`
      - ❌`/config rolegroup list`
      - ❌`/config rolegroup add [rolegroup:string] [roles:string]`
      - ❌`/config rolegroup remove [rolegroup:string] [roles:string]`
      - ❌`/config rolegroup clear [rolegroup:string]`

13. ❌**Ping Role Protection**
    - To prevent users from spamming the ping roles and pinging them in wrong channels, the following can be done:
      - Make the existing ping-roles that users can self-assign unpingable.
      - Create pingable roles that have the same name as the actual unpingable one, that users can ping.
      - Since users are pinging the "fake" roles instead of the actual roles, nobody will get pinged initially.
      - Detect if a user has tried to ping the roles, and if certain conditions are met (eg. cooldown, correct channel, user didn't just join) the bot writes a message that pings the actual role.

### ❌**Ticket System**

14. ❌**Ticket System**
    - Set up a ticket system for user support and issues.
    - Track tickets with chain of custody.

15. ❌**Misc**
    - ✅[Echo Command](Documentation/Commands/echo.md)
---

### **Additional Features**

- **Logs and Data Storage**: Store logs for command executions, moderation actions, and member activity. This will allow for lookup and analysis through a web interface. The configuration should also be stored and be possible to edit in a web interface.
- **Account Age Tracking**: Notify staff when new accounts (less than a week or a month old, should be configurable) join the server.

*Everything should work regardless of what server or how many servers the bot is in, so it's important to store the guild ID with everything that gets stored in the database.*
