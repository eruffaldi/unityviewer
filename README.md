# unitiviewer
3D Unity Data Viewer



User: UDP 
Unity: UDP Server or Websocket Client when WebGL



# When Unity is WebGL

bridge websocket: 
	- server UDP
	- server Websocket

userapp: UDP -> Server
unity: connectes to the bridge

Other Websocket Proxies are TCP only: https://github.com/FWGS/wsproxy