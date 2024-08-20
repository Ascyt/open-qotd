# Purge

The `Purge` commands allow you to delete messages from a channel based on the amount specified or until a specific message is reached.

### Commands

#### 1. `/purge amount`

Purges a specified number of messages from the current channel. The number of messages to be purged can range from 1 to 250, with the default being 10 if no number is specified.

**Usage:**

`/purge amount <amount>`

**Arguments:**

- `<amount (int:1~250)=10>`: The number of messages to purge from the channel.

**Example:**

- `/purge amount 50` - Purges 50 messages from the current channel.

---

#### 2. `/purge until`

Purges messages from the current channel until a certain message is reached. You can specify whether or not the target message itself should be deleted.

**Usage:**

`/purge until [message] <inclusive>`

**Arguments:**

- `<message (message)>`: The message link or ID to delete messages until.
- `[inclusive (bool)=false]`: Whether or not to delete the provided message as well. Defaults to `false`.

**Example:**

- `/purge until 123456789012345678` - Purges all messages after the specified message ID, but does not delete the specified message itself.
- `/purge until 123456789012345678 true` - Purges all messages after and including the specified message ID.

---

**Notes** 
- The `message` argument can accept either a message ID or a message link. 
- This command will purge messages in bulk, making one API request per 100 messages.

---
