# netkit

A UDP room library: one host relaying for up to 16 members, reliable and unreliable messages, join and leave events. Written by a friend of the project's
owner, who gave free permission to use it. The game's side of it is `NETRACE.CPP`.

This copy has the fixes in the friend's `onlineMultiplayerEngine_fixed.zip`, 2026-10-30, handed back to them: joining on a slow connection, one bad datagram
ending a session on Windows, a member sending as another, `NetKit_Count` against `NetKit_MemberAt`. A joiner's broadcast also reaches the host's inbox, which
a game whose host plays needs.

It builds as its own library (`netkit`, C++11, for `<chrono>`): the engine is C++98.
