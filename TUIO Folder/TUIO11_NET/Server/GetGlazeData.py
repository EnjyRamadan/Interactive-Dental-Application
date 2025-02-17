import csv
import firebase_admin
from firebase_admin import credentials, firestore

cred = credentials.Certificate("dentalapp-87611-firebase-adminsdk-9li4a-6f558667b0.json")
firebase_admin.initialize_app(cred)
db = firestore.client()
docs = db.collection("students").stream()
#to get file for each student
for doc in docs:
    data = doc.to_dict()
    id = data.get("student_id", None)
    gaze_data = data.get("eye_coordinates", None)
    print(f"id: {id}")    
    coordinates = [[entry['x'], entry['y']] for entry in gaze_data]
    with open(f'gaze_data_{id}.csv', 'w', newline='') as file:
        writer = csv.writer(file)
        writer.writerow(['Screen_X', 'Screen_Y']) 
        writer.writerows(coordinates)  
'''
# to get a file for all students 
all_gaze_data = []
for doc in docs:
    data = doc.to_dict()
    gaze_data = data.get("eye_coordinates", None)
    
    # If gaze_data exists, append to the aggregated list with the student ID
    if gaze_data:
        for entry in gaze_data:
            all_gaze_data.append([entry['x'], entry['y']])


with open('All_gaze_data.csv', 'w', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(['Screen_X', 'Screen_Y'])
    writer.writerows(all_gaze_data)

# to get a file for all students but with id for each row
all_gaze_data = []
for doc in docs:
    data = doc.to_dict()
    id = data.get("student_id", None)
    gaze_data = data.get("eye_coordinates", None)
    
    # If gaze_data exists, append to the aggregated list with the student ID
    if gaze_data:
        for entry in gaze_data:
            all_gaze_data.append([id, entry['x'], entry['y']])

with open('aggregated_gaze_data.csv', 'w', newline='') as file:
    writer = csv.writer(file)
    # Write headers
    writer.writerow(['Student_ID', 'Screen_X', 'Screen_Y'])
    # Write all collected gaze data
    writer.writerows(all_gaze_data)
'''

