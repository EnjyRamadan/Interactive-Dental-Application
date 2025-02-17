import firebase_admin
from firebase_admin import credentials, firestore

class FirestoreDatabase:
    def __init__(self):
        """
        Initialize Firestore with the provided service account file.
        """
        try:
            cred = credentials.Certificate("hcia-438b7-firebase-adminsdk-xelq2-46df09edbd.json")
            firebase_admin.initialize_app(cred)
            self.db = firestore.client()
            print("Connected to Firestore!")
        except Exception as e:
            print(f"Error initializing Firestore: {e}")
            raise

    def list_collections(self):
        """
        List all top-level collections in the Firestore database.
        """
        try:
            collections = self.db.collections()
            collection_names = [collection.id for collection in collections]
            print("Collections in Firestore:")
            for name in collection_names:
                print(f"- {name}")
            return collection_names
        except Exception as e:
            print(f"Error retrieving collections: {e}")
            return []

    def add_document(self, collection_name, data, doc_id=None):
        """
        Add a document to a collection.
        If doc_id is None, Firestore generates an ID automatically.
        """
        try:
            collection_ref = self.db.collection(collection_name)
            if doc_id:
                doc_ref = collection_ref.document(doc_id)
                doc_ref.set(data)
                print(f"Document '{doc_id}' added/updated successfully.")
            else:
                doc_ref = collection_ref.add(data)
                print(f"Document added with ID: {doc_ref[1].id}")
        except Exception as e:
            print(f"Error adding document: {e}")

    def read_documents(self, collection_name):
        """
        Read all documents from a specific collection.
        """
        try:
            collection_ref = self.db.collection(collection_name)
            docs = collection_ref.stream()
            print(f"Documents in collection '{collection_name}':")
            documents = []
            for doc in docs:
                doc_data = doc.to_dict()
                print(f"- {doc.id}: {doc_data}")
                documents.append({doc.id: doc_data})
            return documents
        except Exception as e:
            print(f"Error retrieving documents: {e}")
            return []

    def update_document(self, collection_name, doc_id, data):
        """
        Update an existing document in a collection.
        """
        try:
            doc_ref = self.db.collection(collection_name).document(doc_id)
            doc_ref.update(data)
            print(f"Document '{doc_id}' updated successfully.")
        except Exception as e:
            print(f"Error updating document: {e}")

    def delete_document(self, collection_name, doc_id):
        """
        Delete a document from a collection.
        """
        try:
            doc_ref = self.db.collection(collection_name).document(doc_id)
            doc_ref.delete()
            print(f"Document '{doc_id}' deleted successfully.")
        except Exception as e:
            print(f"Error deleting document: {e}")


# Example Usage
if __name__ == "__main__":
    pass
    firestore_db = FirestoreDatabase()

    #firestore_db.list_collections()

    # firestore_db.add_document("test_collection", {"name": "John Doe", "age": 30})

    #firestore_db.read_documents("Student")

    # firestore_db.update_document("test_collection", "doc_id_here", {"age": 31})

    # firestore_db.delete_document("test_collection", "doc_id_here")
