from locust import HttpUser, task, between
import random
import string

def random_string(length=10):
    letters = string.ascii_lowercase
    return ''.join(random.choice(letters) for i in range(length))

class UserBehavior(HttpUser):
    wait_time = between(1, 5)

    @task
    def register_user(self):
        self.client.post("/api/auth/register", json={
            "email": f"{random_string(5)}@example.com",
            "username": random_string(8),
            "password": "password123",
            "name": random_string(5),
            "lastName": random_string(5),
            "address": random_string(15),
            "dateOfBirth": "1998-10-23"
        })

class WebsiteUser(HttpUser):
    tasks = [UserBehavior]
    wait_time = between(5, 15)
