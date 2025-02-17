import cv2
import mediapipe as mp
import socket
from dollarpy import Recognizer, Template, Point
import csv
import time
import numpy as np

# Initialize MediaPipe hands
mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands

# Load templates from CSV including direction information
def load_templates_from_csv(csv_file_path):
    loaded_templates = []
    current_gesture = None
    current_points = []
    current_directions = []
    
    with open(csv_file_path, mode='r') as file:
        reader = csv.reader(file)
        next(reader)  # Skip header row
        
        for row in reader:
            gesture_name, point_index, x, y, direction = row[0], int(row[1]), float(row[2]), float(row[3]), float(row[4])
            point = Point(x, y)
            
            if current_gesture != gesture_name:
                if current_gesture is not None:
                    loaded_templates.append((Template(current_gesture, current_points), current_directions))
                current_gesture = gesture_name
                current_points = [point]
                current_directions = [direction]
            else:
                current_points.append(point)
                current_directions.append(direction)
        
        if current_gesture is not None:
            loaded_templates.append((Template(current_gesture, current_points), current_directions))
    
    print("Templates loaded from CSV:", len(loaded_templates))
    return loaded_templates

# Initialize recognizer and load templates
csv_file_path = r"C:\Users\Administrator\source\repos\Interactive-Dental-Application\hand_gesture_templates5.csv"
loaded_templates = load_templates_from_csv(csv_file_path)
recognizer = Recognizer([template for template, _ in loaded_templates])

# Swipe detection thresholds
SWIPE_THRESHOLD = 0.4

def initialize_socket():
    soc = socket.socket()
    hostname = "localhost"  # 127.0.0.1 can also be used
    port = 65434
    soc.bind((hostname, port))
    soc.listen(1)
    print("Waiting for connection...")
    conn, addr = soc.accept()
    print("Device connected:", addr)
    return conn

# Send message through the socket
def send_message(conn, message):
    try:
        encoded_msg = message.encode('utf-8')
        conn.send(encoded_msg)
    except Exception as e:
        print("Failed to send message:", e)

def detect_swipe_direction(points, hand_label):
    """
    Detect swipe direction based on displacement and hand label.
    Returns "Swipe Right" or "Swipe Left" based on conditions.
    """
    x_coords = np.array([point.x for point in points])
    displacement = x_coords[-1] - x_coords[0]
    
    if hand_label == "Right":
        print(f"Right Hand Swipe Right | Displacement: {displacement}")
        return "Swipe Right"
    elif hand_label == "Left":
        print(f"Left Hand Swipe Left | Displacement: {displacement}")
        return "Swipe Left"
    return None

# Use live camera feed for prediction
def recognize_from_camera(conn):
    cap = cv2.VideoCapture(0)
    accumulated_points = []
    accumulated_directions = []
    frame_count = 0
    start_time = time.time()
    
    with mp_hands.Hands(min_detection_confidence=0.5, min_tracking_confidence=0.5) as hands:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break
            
            frame = cv2.flip(frame, 1)
            image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = hands.process(image)
            
            if results.multi_hand_landmarks:
                for hand_landmarks, handedness in zip(results.multi_hand_landmarks, results.multi_handedness):
                    hand_label = handedness.classification[0].label  # 'Left' or 'Right'
                    
                    points = [Point(hand_landmarks.landmark[i].x, hand_landmarks.landmark[i].y) for i in range(21)]
                    directions = [hand_landmarks.landmark[i].z for i in range(21)]  
                    
                    accumulated_points.extend(points)
                    accumulated_directions.extend(directions)
                    frame_count += 1
                    
                    if frame_count >= 20:
                        gesture_start_time = time.time()
                        result = recognizer.recognize(accumulated_points)
                        gesture_end_time = time.time()
                        
                        if isinstance(result, list) and len(result) > 0 and hasattr(result[0], 'name'):
                            print(f"Hand: {hand_label} | Recognized Gesture: {result[0].name} | Score: {result[0].score} | Time taken: {gesture_end_time - gesture_start_time:.2f}s")
                            send_message(conn, result[0].name) 
                        else:
                            if result[0] == "Swipe right" or result[0] == "Swipe left":
                                swipe_direction = detect_swipe_direction(accumulated_points, hand_label)
                                print(f"Hand: {hand_label} | Recognized Gesture: {swipe_direction} | Time taken: {gesture_end_time - gesture_start_time:.2f}s")
                                send_message(conn, swipe_direction)  
                            else:    
                                print(f"Hand: {hand_label} | Recognized Gesture: {result} | Time taken: {gesture_end_time - gesture_start_time:.2f}s")
                                send_message(conn, str(result[0])) 
                        
                        accumulated_points = []
                        accumulated_directions = []
                        frame_count = 0
            
                    mp_drawing.draw_landmarks(frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)
            
            cv2.imshow("Hand Gesture Recognition", frame)
            if cv2.waitKey(10) & 0xFF == ord('q'):
                send_message(conn, "exit")
                break
    
    cap.release()
    cv2.destroyAllWindows()

def main():
    conn = initialize_socket()  # Initialize socket connection
    try:
        recognize_from_camera(conn)  # Start gesture recognition with socket
    finally:
        conn.close()  # Ensure the connection is closed on exit

if __name__ == "__main__":
    main()
