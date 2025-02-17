import socket

soc = socket.socket()
hostname = "localhost"  # 127.0.0.1 can also be used
port = 65434
soc.bind((hostname, port))
soc.listen(5)
conn, addr = soc.accept()
print("Device connected")

while True:
    msg = input("Enter the message: ")  # Get user input
    encoded_msg = msg.encode('utf-8')  # Encode the message as UTF-8 bytes
    conn.send(encoded_msg)  # Send the encoded message

    if msg == "exit":  # Exit condition (no need to pickle)
        break
