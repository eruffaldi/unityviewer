
import socket
import json
import argparse
import numpy as np
import numpy.random
import time
def convertpoint(x):
	return dict(x=x[0],y=x[1],z=x[2])
def main():

	parser = argparse.ArgumentParser(description='Process some integers.')
	parser.add_argument('--x',action="store_true")

	args = parser.parse_args()

	UDP_IP = "127.0.0.1"
	UDP_PORT = 9999

	sock = socket.socket(socket.AF_INET, # Internet
	                     socket.SOCK_DGRAM) # UDP
	while True:
		points = np.random.rand(10,3)*10-2
		what = dict(pointsets=[dict(radius=1,color=dict(g=1.0),printorder="openpose",points=[convertpoint(points[x,:]) for x in range(0,points.shape[0])],id="ciao")])
		MESSAGE = json.dumps(what)

		sock.sendto(MESSAGE, (UDP_IP, UDP_PORT))


		time.sleep(0.5)


if __name__ == '__main__':
	main()