from locust import HttpUser, task, between

class UserBehavior(HttpUser):
    wait_time = between(1, 5)
    token = None

    def on_start(self):
        # Authentication and retrieving the JWT token
        response = self.client.post("/api/auth/login", json={
            "email": "admin@gmail.com",
            "password": "123"
        })
        self.token = response.json()['accessToken']

    @task
    def get_user(self):
        user_id = 5160  # Use the appropriate user ID for testing
        headers = {"Authorization": f"Bearer {self.token}"}
        self.client.get(f"/api/userinfo/{user_id}", headers=headers)

class WebsiteUser(HttpUser):
    tasks = [UserBehavior]
    wait_time = between(5, 15)
    host = "http://51.8.3.51:5000"  # Update this to the correct API Gateway host
