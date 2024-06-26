from locust import HttpUser, task, between
import random
import string
import json

class UserBehavior(HttpUser):
    wait_time = between(1, 5)

    def on_start(self):
        # Authentication and retrieving the JWT token
        response = self.client.post("/api/auth/login", json={
            "email": "admin@gmail.com",
            "password": "123"
        })
        self.token = response.json()['token']

    @task
    def get_user(self):
        user_id = 5160  # You can parameterize this to test different user IDs
        self.client.get(f"/api/userinfo/{user_id}", headers={"Authorization": f"Bearer {self.token}"})

class WebsiteUser(HttpUser):
    tasks = [UserBehavior]
    wait_time = between(5, 15)
