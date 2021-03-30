# vsmod-ModIntegrity

Helps ensure that the client uses only mods approved by the server. It's intended for modpack authors or server owners to restrict which client-only mods their players can use. Mods are checked by mod ID, version, source type (e.g. "zip"), and MD5 fingerprint.

## How It Works

The client, upon joining, sends a report of all of its enabled mods to the server, including any enabled client-only mods. MD5 fingerprints of the mods' files/folders are included (don't worry, it's very fast.) The server checks that the report matches the mods that it's running, plus any client-only mods which have been allowlisted in its config JSON file. If any mods don't match, the player is disconnected with a helpful message which lists which mods caused problems and hints about how to fix things (see below.)

To make things easier for modpack authors or server owners, a command `/modintegrityapprove` can be used to easily add all mods that someone was just kicked for. Getting yourself kicked and using this feature is the easiest way to allowlist the client-only mods in your modpack, as a server owner. Just join your own server, get kicked, then copy-paste the `/modintegrityapprove myPlayerUID` command from the server console to add all your enabled client-side mods.

## Disconnect Messages

Examples of the 4 types of mod issues:

- Unrecognized or banned mod "Make Ice Unslippery" — please disable this mod using the in-game Mod Manager.
- Unrecognized or banned version "0.0.1" for mod "Autopottery" — please update to a known good version, such as: 1.0.0
- Unrecognized or banned source type "DLL" for mod "Drifter Googly Eyes" — please update this mod to use a known good source type, such as: Zip
- Unrecognized or banned fingerprint for mod "Secret Drift Wolves" — please update this mod with a freshly downloaded copy.

The message "Please contact the server owner with any problems or to request new mods." can be customized in `%appdata%/VintagestoryData/ModConfig/ModIntegrity.json`. You should may want to add your own contact information!

## Primary Motivation

Some client-only mods give players an advantage which doesn't match the intended gameplay of a modpack author or server owner. For example, some client-only mods can expose coordinates (which isn't supposed to be allowed on Wilderness Survival playstyle) or show where enemies are through walls. There's probably an X-ray mod out there too...

## Caveats

Doesn't verify any client-side files outside of the mods' source files, so any mod config JSON files can be tampered with, as well as any vanilla assets or dlls.

This is not exactly the most advanced anti-cheat system; however, it should stop "casual cheating". Players who want to cheat will need to explicitly circumvent this system, rather than innocently adding (or forgetting to remove) client-side mods.
