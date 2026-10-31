// Asking the router to open a UDP port (UPnP IGD), for a game hosting an online race: NETUPNP.CPP.
#ifndef NETUPNP_H
#define NETUPNP_H

void NetUpnp_Open(unsigned short port);     // asks in the background; NetUpnp_Result says what came of it
void NetUpnp_Close(void);                   // takes the port off the router again
int NetUpnp_Result(int *ok, char *text, int textSize, char *outside, int outsideSize, char *inside, int insideSize);

#endif
