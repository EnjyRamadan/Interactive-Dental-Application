import socket
import pickle
soc = socket.socket()
hostname="localhost"# 127.0.0.1 #0.0.0.0
port=65434
soc.connect((hostname,port))
while True:
    data = soc.recv(1024)
    res = pickle.loads(data)
    print(res)
    if res == "exit":
        break

soc.close()