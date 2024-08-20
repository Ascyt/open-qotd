# Echo Command

Writes a message or an embed to the current channel or a different channel.

## Plain

Normal, plain text messages.

### Usage

`/echo plain [message] <channel>`

Arguments:
- `[message (string)]`: The message to be sent.
- `<channel (channel)>`: The channel the message should be sent (current channel by default).

## Embed

A message with an embed.

### Usage

`/echo embed [message] <channel> <title> <footer> <author> <color> <withTimestamp>`

Arguments:
- `[message (string)]`: The description of the embed. 
- `<channel (channel)>`: The channel the embed should be sent (current channel by default).
- `<title (string)>`: The title above the description of the embed.
- `<footer (string)>`: The footer below the description of the embed.
- `<author (user)>`: The author of the embed shown above the title.
- `<color (string)>`: The hex color code of the embed.
- `<withTimestamp (boolean)>`: Whether to include the current timestamp or not.
