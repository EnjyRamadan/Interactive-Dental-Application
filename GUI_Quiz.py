import tkinter as tk
from tkinter import messagebox, ttk
from db import FirestoreDatabase

class CustomButton(tk.Button):
    def __init__(self, master=None, **kwargs):
        super().__init__(master,
                        bg='#2c4157',          # Button background
                        fg='#ffffff',          # Text color
                        activebackground='#3c5167',  # Hover background
                        activeforeground='#ffffff',  # Hover text color
                        font=('Arial', 12),
                        relief='raised',
                        borderwidth=2,
                        cursor='hand2',
                        padx=20,
                        pady=10,
                        **kwargs)

class QuizApp:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("MCQ Quiz Application")
        self.root.geometry("800x600")
        self.db = FirestoreDatabase()

        # Define colors
        self.colors = {
            'bg': '#1a2a3a',    # Dark navy
            'fg': '#ffffff',    # White text
            'button': '#2c4157' # Lighter navy for buttons
        }

        # Configure root window
        self.root.configure(bg=self.colors['bg'])

        # Configure style for other elements
        self.style = ttk.Style()
        self.style.configure('Custom.TFrame', background=self.colors['bg'])
        self.style.configure('Custom.TLabel',
                             background=self.colors['bg'],
                             foreground=self.colors['fg'],
                             font=('Arial', 12))
        self.style.configure('Title.TLabel',
                             background=self.colors['bg'],
                             foreground=self.colors['fg'],
                             font=('Arial', 18, 'bold'))
        self.style.configure('Custom.TRadiobutton',
                             background=self.colors['bg'],
                             foreground=self.colors['fg'],
                             font=('Arial', 12))

        # Load data from Firestore
        self.users = self.load_users()
        self.questions = self.load_questions()

        self.current_user = None
        self.current_question = 1
        self.user_answers = {}

        self.create_login_page()

    def load_users(self):
        """
        Load user data (admin and student) from Firestore.
        """
        admins = self.db.read_documents("Admin")
        students = self.db.read_documents("Student")

        users = {}

        # Parse admin credentials
        for admin in admins:
            for admin_name, password in admin.items():
                if isinstance(password, dict):
                    username = password['username']
                    password = list(password.values())[0]  # Extract nested password
                users[username] = {"password": password, "role": "admin"}

        # Parse student credentials
        for student in students:
            for student_id, data in student.items():
                users[data["user"]] = {
                    "password": data["password"],
                    "role": "student",
                    "name": data.get("Name", "Unknown")
                }

        return users

    def load_questions(self):
        """
        Load questions from Firestore and remap keys to integers.
        """
        question_data = self.db.read_documents("Questions")
        questions = {}
        self.question_keys = []  # To track the order of questions

        for document in question_data:
            for doc_id, q_details in document.items():  # Each document is a dictionary
                choices = [value for key, value in q_details.items() if key.startswith("answer")]
                correct_answer = q_details.get("correct", "").strip()

                if choices and correct_answer in choices:
                    questions[doc_id] = {
                        "question": q_details.get("question", "No question provided"),
                        "choices": choices,
                        "correct": choices.index(correct_answer),
                    }
                    self.question_keys.append(doc_id)
                else:
                    print(f"Skipping invalid question data: {q_details}")

        return questions



    def create_login_page(self):
        self.clear_window()

        main_frame = ttk.Frame(self.root, style='Custom.TFrame')
        main_frame.pack(expand=True, fill='both', padx=50, pady=50)
        main_frame.grid_columnconfigure(0, weight=1)

        # Title
        title_label = ttk.Label(main_frame, text="Quiz Login", style='Title.TLabel')
        title_label.grid(row=0, column=0, pady=(0, 40))

        # Login form
        login_frame = ttk.Frame(main_frame, style='Custom.TFrame')
        login_frame.grid(row=1, column=0)

        ttk.Label(login_frame, text="Username:", style='Custom.TLabel').grid(row=0, column=0, pady=10)
        username_entry = ttk.Entry(login_frame, width=30, font=('Arial', 12))
        username_entry.grid(row=1, column=0, pady=5)

        ttk.Label(login_frame, text="Password:", style='Custom.TLabel').grid(row=2, column=0, pady=10)
        password_entry = ttk.Entry(login_frame, show="*", width=30, font=('Arial', 12))
        password_entry.grid(row=3, column=0, pady=5)

        def login():
            self.questions = self.load_questions()
            username = username_entry.get()
            password = password_entry.get()
            print(self.users)
            if username in self.users.keys() and self.users[username]["password"] == password:
                self.current_user = username
                if self.users[username]["role"] == "admin":
                    self.create_admin_page()
                else:
                    self.create_quiz_page()
            else:
                messagebox.showerror("Error", "Invalid credentials")

        login_button = CustomButton(login_frame, text="Login", command=login)
        login_button.grid(row=4, column=0, pady=30)

    def create_admin_page(self):
        self.clear_window()

        main_frame = ttk.Frame(self.root, style='Custom.TFrame')
        main_frame.pack(expand=True, fill='both', padx=50, pady=50)
        main_frame.grid_columnconfigure(0, weight=1)

        # Title
        ttk.Label(main_frame, text="Admin Panel", style='Title.TLabel').grid(row=0, column=0, pady=(0, 40))

        # Add "Add Question" Button
        CustomButton(main_frame, text="Add Question", command=self.add_question).grid(row=1, column=0, pady=10)
    
        CustomButton(main_frame, text="Update Question", command=self.update_question).grid(row=2, column=0, pady=10)

        CustomButton(main_frame, text="Delete Question", command=self.delete_question).grid(row=3, column=0, pady=10)


        # Add "Logout" Button
        CustomButton(main_frame, text="Logout", command=self.create_login_page).grid(row=4, column=0, pady=10)


        ttk.Label(main_frame, text="Admin Panel", style='Title.TLabel').grid(row=0, column=0, pady=(0, 40))

    def add_question(self):
        add_window = tk.Toplevel(self.root)
        add_window.title("Add Question")
        add_window.geometry("500x600")
        add_window.configure(bg=self.colors['bg'])

        add_frame = ttk.Frame(add_window, style='Custom.TFrame')
        add_frame.pack(expand=True, fill='both', padx=30, pady=30)

        ttk.Label(add_frame, text="Add New Question", style='Title.TLabel').pack(pady=(0, 20))

        ttk.Label(add_frame, text="Question:", style='Custom.TLabel').pack(pady=5)
        question_entry = ttk.Entry(add_frame, width=50, font=('Arial', 12))
        question_entry.pack(pady=(0, 20))

        choices = []
        for i in range(4):
            ttk.Label(add_frame, text=f"Choice {i+1}:", style='Custom.TLabel').pack(pady=5)
            choice_entry = ttk.Entry(add_frame, width=50, font=('Arial', 12))
            choice_entry.pack(pady=(0, 10))
            choices.append(choice_entry)

        ttk.Label(add_frame, text="Correct Answer (1-4):", style='Custom.TLabel').pack(pady=5)
        correct_entry = ttk.Entry(add_frame, width=10, font=('Arial', 12))
        correct_entry.pack(pady=(0, 20))

        def save_question():
            # Gather the input data
            question_text = question_entry.get()
            choice_values = [choice.get() for choice in choices]
            correct_index = int(correct_entry.get()) - 1  # Correct answer is 1-based

            # Validate the input
            if not question_text or any(not choice for choice in choice_values) or correct_index not in range(len(choice_values)):
                messagebox.showerror("Error", "Please ensure all fields are filled correctly.")
                return

            # Prepare the question data for Firebase
            question_data = {
                "question": question_text,
                "correct": choice_values[correct_index]
            }
            for idx, choice in enumerate(choice_values):
                question_data[f"answer{idx + 1}"] = choice

            try:
                # Save the question to Firestore
                print(question_data)
                self.db.add_document("Questions", question_data)
                messagebox.showinfo("Success", "Question added successfully to Firebase!")
                add_window.destroy()
            except Exception as e:
                messagebox.showerror("Error", f"Failed to save question: {e}")

        # Add a button to save the question
        CustomButton(add_frame, text="Save Question", command=save_question).pack(pady=20)
    

    def create_quiz_page(self):
        self.clear_window()

        if not self.questions:
            messagebox.showerror("Error", "No questions available. Please contact the admin.")
            self.create_login_page()
            return

        question_id = self.question_keys[self.current_question - 1]  # Map index to key
        question = self.questions[question_id]

        main_frame = ttk.Frame(self.root, style='Custom.TFrame')
        main_frame.pack(expand=True, fill='both', padx=50, pady=50)
        main_frame.grid_columnconfigure(0, weight=1)

        ttk.Label(main_frame,
                text=f"Question {self.current_question} of {len(self.questions)}",
                style='Custom.TLabel').grid(row=0, column=0, pady=(0, 20))

        question_text = ttk.Label(main_frame,
                                text=question["question"],
                                style='Title.TLabel',
                                wraplength=500)
        question_text.grid(row=1, column=0, pady=(0, 40))

        choices_frame = ttk.Frame(main_frame, style='Custom.TFrame')
        choices_frame.grid(row=2, column=0, pady=(0, 40))

        selected_choice = tk.IntVar()
        for i, choice in enumerate(question["choices"]):
            ttk.Radiobutton(choices_frame,
                            text=choice,
                            variable=selected_choice,
                            value=i,
                            style='Custom.TRadiobutton').pack(pady=10)

        if question_id in self.user_answers:
            selected_choice.set(self.user_answers[question_id])

        def save_answer():
            self.user_answers[question_id] = selected_choice.get()

        def next_question():
            save_answer()
            if self.current_question < len(self.question_keys):
                self.current_question += 1
                self.create_quiz_page()
            else:
                self.show_results()

        def prev_question():
            save_answer()
            if self.current_question > 1:
                self.current_question -= 1
                self.create_quiz_page()

        nav_frame = ttk.Frame(main_frame, style='Custom.TFrame')
        nav_frame.grid(row=3, column=0, pady=20)

        CustomButton(nav_frame, text="Previous", command=prev_question).pack(side='left', padx=10)
        CustomButton(nav_frame, text="Next", command=next_question).pack(side='left', padx=10)

        if self.current_question == len(self.question_keys):
            CustomButton(main_frame, text="Submit Quiz",
                        command=self.show_results).grid(row=4, column=0, pady=20)

    def update_question(self):
        update_window = tk.Toplevel(self.root)
        update_window.title("Update Question")
        update_window.geometry("600x600")
        update_window.configure(bg=self.colors['bg'])

        update_frame = ttk.Frame(update_window, style='Custom.TFrame')
        update_frame.pack(expand=True, fill='both', padx=30, pady=30)

        ttk.Label(update_frame, text="Update Question", style='Title.TLabel').pack(pady=(0, 20))

        # Dropdown to select a question
        question_ids = list(self.questions.keys())
        question_var = tk.StringVar()
        question_dropdown = ttk.Combobox(update_frame, textvariable=question_var, state="readonly")
        question_dropdown["values"] = [f"Question {qid}: {self.questions[qid]['question']}" for qid in question_ids]
        question_dropdown.pack(pady=10)

        # Question Entry
        ttk.Label(update_frame, text="Updated Question Text:", style='Custom.TLabel').pack(pady=5)
        question_entry = ttk.Entry(update_frame, width=50, font=('Arial', 12))
        question_entry.pack(pady=10)

        # Choices
        choices = []
        for i in range(4):
            ttk.Label(update_frame, text=f"Updated Choice {i+1}:", style='Custom.TLabel').pack(pady=5)
            choice_entry = ttk.Entry(update_frame, width=50, font=('Arial', 12))
            choice_entry.pack(pady=5)
            choices.append(choice_entry)

        # Correct Answer
        ttk.Label(update_frame, text="Updated Correct Answer (1-4):", style='Custom.TLabel').pack(pady=5)
        correct_entry = ttk.Entry(update_frame, width=10, font=('Arial', 12))
        correct_entry.pack(pady=10)

        def load_question():
            # Load selected question details
            selected_id = question_ids[question_dropdown.current()]
            selected_question = self.questions[selected_id]
            question_entry.delete(0, tk.END)
            question_entry.insert(0, selected_question['question'])
            for i, choice in enumerate(selected_question['choices']):
                choices[i].delete(0, tk.END)
                choices[i].insert(0, choice)
            correct_entry.delete(0, tk.END)
            correct_entry.insert(0, selected_question['correct'] + 1)

        def save_updated_question():
            selected_id = question_ids[question_dropdown.current()]
            updated_question = question_entry.get()
            updated_choices = [choice.get() for choice in choices]
            updated_correct = int(correct_entry.get()) - 1

            # Validate the input
            if not updated_question or any(not choice for choice in updated_choices) or updated_correct not in range(4):
                messagebox.showerror("Error", "Please ensure all fields are filled correctly.")
                return

            # Prepare updated question data
            updated_data = {
                "question": updated_question,
                "correct": updated_choices[updated_correct]
            }
            for i, choice in enumerate(updated_choices):
                updated_data[f"answer{i + 1}"] = choice

            try:
                self.db.update_document("Questions", selected_id, updated_data)
                self.questions = self.load_questions()  # Reload questions
                messagebox.showinfo("Success", "Question updated successfully!")
                update_window.destroy()
            except Exception as e:
                messagebox.showerror("Error", f"Failed to update question: {e}")

        load_button = CustomButton(update_frame, text="Save Updates", command=save_updated_question)
        load_button.pack(pady=10)

    def delete_question(self):
        delete_window = tk.Toplevel(self.root)
        delete_window.title("Delete Question")
        delete_window.geometry("400x300")
        delete_window.configure(bg=self.colors['bg'])

        delete_frame = ttk.Frame(delete_window, style='Custom.TFrame')
        delete_frame.pack(expand=True, fill='both', padx=30, pady=30)

        ttk.Label(delete_frame, text="Delete Question", style='Title.TLabel').pack(pady=(0, 20))

        # Dropdown to select a question
        question_ids = list(self.questions.keys())
        question_var = tk.StringVar()
        question_dropdown = ttk.Combobox(delete_frame, textvariable=question_var, state="readonly")
        question_dropdown["values"] = [f"Question {self.questions[qid]['question']}" for qid in question_ids]
        question_dropdown.pack(pady=10)

        def delete_selected_question():
            selected_id = question_ids[question_dropdown.current()]

            try:
                # Delete from Firestore
                self.db.delete_document("Questions", selected_id)
                self.questions = self.load_questions()
                messagebox.showinfo("Success", "Question deleted successfully!")
                delete_window.destroy()
            except Exception as e:
                messagebox.showerror("Error", f"Failed to delete question: {e}")

        delete_button = CustomButton(delete_frame, text="Delete Question", command=delete_selected_question)
        delete_button.pack(pady=20)

    def show_results(self):
        self.clear_window()

        main_frame = ttk.Frame(self.root, style='Custom.TFrame')
        main_frame.pack(expand=True, fill='both', padx=50, pady=50)
        main_frame.grid_columnconfigure(0, weight=1)

        correct_answers = 0
        for q_id, answer in self.user_answers.items():
            if answer == self.questions[q_id]["correct"]:
                correct_answers += 1

        score = (correct_answers / len(self.questions)) * 100

        ttk.Label(main_frame, text="Quiz Results", style='Title.TLabel').grid(row=0, column=0, pady=(0, 40))
        ttk.Label(main_frame, text=f"Score: {score:.2f}%", style='Custom.TLabel').grid(row=1, column=0, pady=10)
        ttk.Label(main_frame, text=f"Correct Answers: {correct_answers}/{len(self.questions)}",
                style='Custom.TLabel').grid(row=2, column=0, pady=10)

        CustomButton(main_frame, text="Return to Login",
                    command=self.create_login_page).grid(row=3, column=0, pady=30)

    def clear_window(self):
        for widget in self.root.winfo_children():
            widget.destroy()

    def run(self):
        self.root.mainloop()


if __name__ == "__main__":
    app = QuizApp()
    app.run()
