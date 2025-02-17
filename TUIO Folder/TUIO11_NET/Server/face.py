import face_recognition
import cv2
import numpy as np
import firebase_admin
from firebase_admin import credentials, firestore
import bluetooth
import socket
import threading

def get_live_face_encoding():
    video_capture = cv2.VideoCapture(0)

    face_encodings = []

    print("Detecting faces. Please position your face in front of the camera...")
    while True:
        # Capture frame from the webcam
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to grab frame. Please check your webcam.")
            break

        # Convert the frame to RGB and make it contiguous
        rgb_frame = np.ascontiguousarray(frame[:, :, ::-1])

        # Detect face locations
        face_locations = face_recognition.face_locations(rgb_frame)

        # Detect face encodings
        face_encodings = face_recognition.face_encodings(rgb_frame, face_locations)

        # Draw rectangles around detected faces
        for (top, right, bottom, left) in face_locations:
            cv2.rectangle(frame, (left, top), (right, bottom), (0, 255, 0), 2)

        # Show the frame with rectangles
        cv2.imshow("Face Detection", frame)

        # Automatically break if at least one face encoding is detected
        if face_encodings:
            print(f"Detected {len(face_encodings)} face(s). Capturing encodings...")
            break

        # Exit loop if 'q' is pressed
        key = cv2.waitKey(1) & 0xFF
        if key == ord('q'):
            print("Exiting without capturing.")
            face_encodings = []
            break

    # Release the webcam and close the display window
    video_capture.release()
    cv2.destroyAllWindows()
    flat_face_encodings = face_encodings[0].tolist() if face_encodings else [] 
    return flat_face_encodings
def compare_faces(face1,face2,tolerance=0.6):
    stored_encoding = np.array(face1)
    new_encoding = np.array(face2)
    results = face_recognition.compare_faces([stored_encoding], new_encoding, tolerance)    
    return results[0]

def listen_for_messages(sock):
    #Connect to Firestore and ffetch the data            
    cred = credentials.Certificate("dentalapp-87611-firebase-adminsdk-9li4a-7c66415331.json")
    firebase_admin.initialize_app(cred)
    db = firestore.client()
    while True:
        data = sock.recv(1024)  # Receive data from the server
        if data:
            message = data.decode()  # Decode the byte data to string
            print("Received:", message)
            parts = message.split(",")
            action = parts[0]
            
            # Check the received message and respond accordingly
            if action == "login":
                MAC = parts[2]
                doc_ref = db.collection("devices").document(MAC)
                doc = doc_ref.get()

                #login
                if doc.exists:
                    # Extract the data
                    data = doc.to_dict()
                    face_coordinates = data.get("face_coordinates", None)
                    if face_coordinates is not None:
                        if compare_faces(get_live_face_encoding(),face_coordinates):
                            device_role = data.get("role", None)
                            progress = data.get("progress", None)
                            sock.sendall("logged,"+parts[1]+","+parts[2]+","+device_role+","+progress.encode())
                        else:
                            sock.sendall("try again,"+parts[1]+","+parts[2].encode())
                    else:
                        sock.sendall("try again,"+parts[1]+","+parts[2].encode())
                #sign up
                else:
                    device_MAC = parts[2]
                    device_name = parts[1]
                    device_role = "student"
                    face_coordinates = get_live_face_encoding()
                    progress = "0%"
                    doc_ref = db.collection("devices").document(device_MAC)
                    doc_ref.set({
                        "MAC":device_MAC,
                        "device_name": device_name,
                        "role": device_role, 
                        "face_coordinates": face_coordinates,
                        "progress": progress
                    })
                    sock.sendall("logged,"+parts[1]+","+parts[2]+","+device_role+","+progress.encode())                                    
            elif message == "progress":
                device_MAC = parts[1]
                doc_ref = db.collection("devices").document(device_MAC)
                doc_ref.update({
                    "progress": parts[2],  # Update progress to 75%
                })
            else:
                sock.sendall(f"Received: {message}".encode())  # Echo other messages

def send_messages(sock):
    #find nearby devices
    nearby_devices = bluetooth.discover_devices(lookup_names=True)
    print("Nearby Devices:", nearby_devices)
    nearby_devices_tuples = [(name, addr) for addr, name in nearby_devices]
    message = ", ".join(f"{name},{addr}" for name, addr in nearby_devices_tuples)
    print(message)
    sock.sendall(message.encode())  # Send message to the server

def main():
    host = '127.0.0.1'  # Server IP address
    port = 5000          # Server port
    cred = credentials.Certificate("dentalapp-87611-firebase-adminsdk-9li4a-6f558667b0.json")

    # Create a socket object and connect to the server
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((host, port))

    try:
        # Start threads for listening and sending messages
        threading.Thread(target=send_messages, args=(sock,)).start()
        threading.Thread(target=listen_for_messages, args=(sock,)).start()
    finally:
        sock.close()  # Ensure the socket is closed on exit

if __name__ == "__main__":
    main()


#find nearby devices
#nearby_devices = bluetooth.discover_devices(lookup_names=True)
#print("Nearby Devices:", nearby_devices)
#nearby_devices_tuples = [(name, addr) for addr, name in nearby_devices]
#message = ", ".join(name for name, _ in nearby_devices_tuples)
#print(message)



# Connect to Firestore and ffetch the data
#cred = credentials.Certificate("dentalapp-87611-firebase-adminsdk-9li4a-7c66415331.json")
#firebase_admin.initialize_app(cred)
#db = firestore.client()
#index = 0 #the recieved index
#MAC = nearby_devices_tuples[index][1]
#doc_ref = db.collection("devices").document(MAC)
#doc = doc_ref.get()



#login
#if doc.exists:
#    # Extract the data
#    data = doc.to_dict()
#    face_coordinates = data.get("face_coordinates", None)
#    device_name = data.get("face_coordinates", None)
#    if face_coordinates is not None:
#        # Convert the list back to a numpy array (if needed)
#        face_coordinates_array = np.array(face_coordinates)
#        print(f"Face coordinates for {device_name}:")
#        print(face_coordinates_array)
#    else:
#        print(f"Face coordinates not found for {device_name}.")
#        
#        
##sign up
#else:
#    print(f"Device not found!")
