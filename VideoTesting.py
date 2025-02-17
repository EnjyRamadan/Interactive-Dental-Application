import cv2
import mediapipe as mp
from dollarpy import Recognizer, Template, Point
import csv
import time

mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands

def load_templates_from_csv(csv_file_path):
    loaded_templates = []
    current_gesture = None
    current_points = []

    with open(csv_file_path, mode='r') as file:
        reader = csv.reader(file)
        next(reader)

        for row in reader:
            gesture_name, point_index, x, y = row[0], int(row[1]), float(row[2]), float(row[3])
            point = Point(x, y)

            if current_gesture != gesture_name:
                if current_gesture is not None:
                    loaded_templates.append(Template(current_gesture, current_points))
                current_gesture = gesture_name
                current_points = [point]
            else:
                current_points.append(point)

        if current_gesture is not None:
            loaded_templates.append(Template(current_gesture, current_points))

    print("Templates loaded from CSV:", len(loaded_templates))
    return loaded_templates

csv_file_path = 'hand_gesture_templates.csv'
loaded_templates = load_templates_from_csv(csv_file_path)
recognizer = Recognizer(loaded_templates)

def recognize_from_video(video_path):
    cap = cv2.VideoCapture(video_path)
    all_points = []

    with mp_hands.Hands(min_detection_confidence=0.5, min_tracking_confidence=0.5) as hands:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = hands.process(image)

            if results.multi_hand_landmarks:
                hand_landmarks = results.multi_hand_landmarks[0]
                points = [Point(hand_landmarks.landmark[i].x, hand_landmarks.landmark[i].y) for i in range(21)]
                all_points.extend(points)

            cv2.imshow("Video Processing", frame)
            if cv2.waitKey(10) & 0xFF == ord('q'):
                break

    cap.release()
    cv2.destroyAllWindows()

    print(f"Total points collected from video: {len(all_points)}")

    if all_points:
        start_time = time.time()
        result = recognizer.recognize(all_points)
        end_time = time.time()

        if result and isinstance(result[0], Template):
            print(f"Recognized Gesture: {result[0].name} | Score: {result[0].score} | Time taken: {end_time - start_time:.2f}s")
        elif result:
            print(f"Recognized Gesture: {result} | Time taken: {end_time - start_time:.2f}s")
        else:
            print("No match - Check if points match any of the templates")
    else:
        print("No hand points detected in video.")

test_video_path = "Hand Gestures/Swipe right/L0.webm"
recognize_from_video(test_video_path)
