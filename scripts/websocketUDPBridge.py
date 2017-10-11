# WS Server - UDP Server
# Taken online, refactored 
#
# Emanuele Ruffaldi 2017
import time, sys, os, pkg_resources

import SocketServer

from twisted.python import log
from twisted.internet import reactor
from twisted.application import service
from twisted.internet.protocol import DatagramProtocol, Protocol, Factory

from twisted.web.server import Site
from twisted.web.static import File

from autobahn.twisted.websocket import WebSocketServerProtocol, \
                                       WebSocketServerFactory

from autobahn.twisted.resource import WebSocketResource
                             
# constants

SERVER_HTTP_PORT = 9000
SERVER_HTTP_RESOURCES = 'web'
BINARYWS = 0
CLIENT_IP = '127.0.0.1'
CLIENT_UDP_PORT = 7500

# [HTTP] > [CLIENT WS] > [SERVER WS] > bridge > [SERVER UDP] > [CLIENT UDP]

class Bridge():

  def __init__(self):
    self.udpServer = None
    self.wsServer = None

  def setUdpServer(self, udpServer):
    self.udpServer = udpServer

  def setWebsocketServer(self, wsServer):
    self.wsServer = wsServer

  def udpToWebsocket(self, data):
    global BINARYWS
    if self.wsServer is not None:
      print "relay",self.wsServer
      self.wsServer.sendMessage(data, BINARYWS)
    else:
      print "not relay"

  def websocketToUdp(self, data):
    if self.udpServer is not None:
      self.udpServer.transport.write(data, (CLIENT_IP, CLIENT_UDP_PORT))

# udp server

class UDPServer(DatagramProtocol):

  def __init__(self, bridge):
    self.bridge = bridge
    self.bridge.setUdpServer(self)

  def datagramReceived(self, data, (host, port)):
    global CLIENT_IP, CLIENT_UDP_PORT
    CLIENT_IP = host
    CLIENT_UDP_PORT = port
    self.bridge.udpToWebsocket(data)

# websocket server

class BridgedWebSocketServerFactory(WebSocketServerFactory):

  def __init__(self, url, debug, debugCodePaths, bridge):
    WebSocketServerFactory.__init__(self, url)
    self.bridge = bridge

class WebSocketServer(WebSocketServerProtocol):

  def onOpen(self):
    print 'WebSocket connection open.'
    self.factory.bridge.setWebsocketServer(self)

  def onConnect(self, request):
    print 'Client connecting: {0}'.format(request.peer)

  def onMessage(self, payload, isBinary):
    self.factory.bridge.websocketToUdp(payload)

  def onClose(self, wasClean, code, reason):
    print 'WebSocket connection closed: {0}'.format(reason)
    self.factory.bridge.setWebsocketServer(None)



# initalize servers

if __name__ == '__main__':
  import argparse

  parser = argparse.ArgumentParser(description='WebSocket - UDP Server bridge: listen UDP and relays to WS ')
  parser.add_argument('--udp-port',default=8051,help="udp port for listening")
  parser.add_argument('--ws-port',default=8001,help="WS port for listening")
  parser.add_argument('--host',default="localhost",help="WS host for listening")
  parser.add_argument('--binary',action="store_true")

  args = parser.parse_args()
  print "UDP Server on ",args.udp_port
  print "WS  Server on ",args.host,args.ws_port
  bridge = Bridge()

  log.startLogging(sys.stdout)

  # websocket setup

  wsAddress = 'ws://%s:%d' % (args.host, args.ws_port)

  factory = BridgedWebSocketServerFactory(wsAddress, False, False, bridge)
  factory.protocol = WebSocketServer
  reactor.listenTCP(args.ws_port, factory)

  # http setup
  if False:
    webdir = os.path.abspath(SERVER_HTTP_RESOURCES)
    site = Site(File(webdir))
    reactor.listenTCP(SERVER_HTTP_PORT, site)

  # udp setup

  reactor.listenUDP(args.udp_port, UDPServer(bridge))

  # start session

  reactor.run()