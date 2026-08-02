import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'
import { getStorage } from 'firebase/storage'

const firebaseConfig = {
  apiKey: "AIzaSyDiwdOfnpuIHMFQf2buHRr5Ot1LqOy-45E",
  authDomain: "dlistudios.firebaseapp.com",
  projectId: "dlistudios",
  storageBucket: "dlistudios.firebasestorage.app",
  messagingSenderId: "1084445387497",
  appId: "1:1084445387497:web:2bfbd40542533ecf219e78",
  measurementId: "G-157PXGWCS3"
}

const app = initializeApp(firebaseConfig)
export const auth = getAuth(app)
export const storage = getStorage(app)
export default app
