import cv2
import numpy as np
import dlib
import csv
import firebase_admin
from firebase_admin import credentials, firestore

cap = cv2.VideoCapture(1)
detector = dlib.get_frontal_face_detector()
predictor = dlib.shape_predictor("shape_predictor_68_face_landmarks.dat")
#https://drive.google.com/file/d/1658b6TnGf5_hh8F-KcS1Y_xV_F02gXNL/view?usp=sharing 
def pupil_position(eye_points, facial_landmarks, gray):
    eye_region = np.array([(facial_landmarks.part(point).x, facial_landmarks.part(point).y) for point in eye_points])
    min_x = np.min(eye_region[:, 0])
    max_x = np.max(eye_region[:, 0])
    min_y = np.min(eye_region[:, 1])
    max_y = np.max(eye_region[:, 1])
    
    eye = gray[min_y:max_y, min_x:max_x]
    _, threshold_eye = cv2.threshold(eye, 30, 255, cv2.THRESH_BINARY_INV)
    contours, _ = cv2.findContours(threshold_eye, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
    if contours and len(contours) > 0:
        contour = max(contours, key=cv2.contourArea)
        M = cv2.moments(contour)
        if M['m00'] != 0:
            cx = int(M['m10'] / M['m00'])  
            cy = int(M['m01'] / M['m00'])  
            return (min_x + cx, min_y + cy)  
    return (min_x + (max_x - min_x) // 2, min_y + (max_y - min_y) // 2)

def calibrate_gaze():
    calibration_points = [(0.5, 0.5), (0.1, 0.1), (0.9, 0.1), (0.1, 0.9), (0.9, 0.9)]  
    calibration_data = []
    for x, y in calibration_points:
        
        cv2.waitKey(2000)  
        
        _, frame = cap.read()
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        faces = detector(gray)
        for face in faces:
            landmarks = predictor(gray, face)
            left_pupil = pupil_position(range(36, 42), landmarks, gray)
            right_pupil = pupil_position(range(42, 48), landmarks, gray)
            screen_x = np.mean([left_pupil[0], right_pupil[0]]) / frame.shape[1] * 1366
            screen_y = np.mean([left_pupil[1], right_pupil[1]]) / frame.shape[0] * 768
            calibration_data.append(((x, y), (screen_x, screen_y)))
    return calibration_data


gaze_data = []

if __name__ == "__main__":
    calibration_data = calibrate_gaze()

    while True:
        _, frame = cap.read()
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        faces = detector(gray)

        for face in faces:
            landmarks = predictor(gray, face)
            left_pupil = pupil_position(range(36, 42), landmarks, gray)
            right_pupil = pupil_position(range(42, 48), landmarks, gray)
            screen_x = np.mean([left_pupil[0], right_pupil[0]]) / frame.shape[1] * 1920
            screen_y = np.mean([left_pupil[1], right_pupil[1]]) / frame.shape[0] * 1080
            gaze_data.append({"x":int(screen_x),"y": int(screen_y)})

        cv2.imshow("Frame", frame)
        if cv2.waitKey(1) & 0xFF == 27:  # ESC key
            break

    cap.release()
    cv2.destroyAllWindows()
    
    cred = credentials.Certificate("dentalapp-87611-firebase-adminsdk-9li4a-6f558667b0.json")
    firebase_admin.initialize_app(cred)
    db = firestore.client()
    student_id = "220855_3"
    student_name = "test2"
    student_exp = "TUIO"
    eye_coordinates = gaze_data
    print(gaze_data)
    doc_ref = db.collection("students").document(student_id)
    doc_ref.set({
        "student_id":student_id,
        "student_name": student_name,
        "student_exp": student_exp, 
        "eye_coordinates": eye_coordinates,
    })
    #with open('gaze_data.csv', 'w', newline='') as file:
    #    writer = csv.writer(file)
    #    writer.writerow(['Screen_X', 'Screen_Y'])
    #    writer.writerows(gaze_data)
    #print("Enhanced gaze data saved to 'enhanced_gaze_data.csv'")
