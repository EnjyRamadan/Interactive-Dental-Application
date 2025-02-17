import cv2
import mediapipe as mp
from dollarpy import Recognizer, Template, Point
import csv
import time
import socket

# Initialize MediaPipe hands
mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands

# Load templates from CSV for recognizer
def load_templates_from_csv(csv_file_path):
    loaded_templates = []
    current_gesture = None
    current_points = []

    # Open the CSV and read each row
    with open(csv_file_path, mode='r') as file:
        reader = csv.reader(file)
        next(reader)  # Skip header row

        for row in reader:
            gesture_name, point_index, x, y = row[0], int(row[1]), float(row[2]), float(row[3])
            point = Point(x, y)

            # Check if we're still on the same gesture
            if current_gesture != gesture_name:
                # If not, save the previous gesture's template if it exists
                if current_gesture is not None:
                    loaded_templates.append(Template(current_gesture, current_points))
                # Start a new gesture template
                current_gesture = gesture_name
                current_points = [point]
            else:
                # Continue adding points to the current gesture template
                current_points.append(point)

        # Add the last gesture template
        if current_gesture is not None:
            loaded_templates.append(Template(current_gesture, current_points))

    print("Templates loaded from CSV:", len(loaded_templates))
    return loaded_templates

# Initialize recognizer and load templates
csv_file_path = 'hand_gesture_templates.csv'
loaded_templates = load_templates_from_csv(csv_file_path)
recognizer = Recognizer(loaded_templates)

soc = socket.socket()
hostname = "localhost"
port = 65434
soc.bind((hostname, port))
soc.listen(5)
print("Waiting for connection...")
conn, addr = soc.accept()
print("Device connected")

# Use live camera feed for prediction and send result over socket
def recognize_from_camera():
    cap = cv2.VideoCapture(0)
    with mp_hands.Hands(min_detection_confidence=0.5, min_tracking_confidence=0.5) as hands:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            # Process frame
            frame = cv2.flip(frame, 1)
            image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = hands.process(image)

            if results.multi_hand_landmarks:
                # Extract points from the first detected hand
                hand_landmarks = results.multi_hand_landmarks[0]
                points = [Point(hand_landmarks.landmark[i].x, hand_landmarks.landmark[i].y) for i in range(21)]
                
                # Recognize the gesture for the current frame only
                start_time = time.time()
                result = recognizer.recognize(points)
                end_time = time.time()
                
                # Send recognized gesture over socket
                if result:
                    message = f"Recognized Gesture: {result} | Time taken: {end_time - start_time:.2f}s"
                    print(message)  # Print for local reference
                    conn.sendall(result[0].encode('utf-8'))  # Send result over socket
                else:
                    no_match_message = "No match"
                    print(no_match_message)
                    conn.sendall(no_match_message.encode('utf-8'))  # Send "No match" if gesture is not recognized

                # Optionally, draw landmarks on the video for reference (can be removed if not needed)
                mp_drawing.draw_landmarks(frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)

            # Display the video feed without any overlay text
            cv2.imshow("Hand Gesture Recognition", frame)

            if cv2.waitKey(10) & 0xFF == ord('q'):
                break

    cap.release()
    cv2.destroyAllWindows()
    conn.close()  # Close connection after stopping
    soc.close()

# Start the recognition from camera
recognize_from_camera()
