import socket

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect(("127.0.0.1", 1110))

print("Servidor dice:", s.recv(4096))

while True:
    msg = s.recv(4096)
    print("Tick:", msg.decode())
