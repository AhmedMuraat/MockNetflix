from locust import HttpUser, task, between, events

class UserBehavior(HttpUser):
    wait_time = between(1, 5)
    token = None

    def on_start(self):
        # Authentication and retrieving the JWT token
        response = self.client.post("/api/auth/login", json={
            "email": "admin@gmail.com",
            "password": "123"
        })

        if response.status_code == 200:
            self.token = response.json().get('accessToken')
            print(f"Successfully authenticated. Token: {self.token}")
        else:
            print(f"Failed to authenticate. Status code: {response.status_code}")
            print(f"Response content: {response.content}")

    @task
    def get_user(self):
        if not self.token:
            print("No token available. Skipping the request.")
            return

        user_id = 5160  # Use the appropriate user ID for testing
        headers = {"Authorization": f"Bearer {self.token}"}
        response = self.client.get(f"/api/users/{user_id}", headers=headers)

        if response.status_code == 200:
            print(f"Successfully fetched user info for user ID {user_id}")
        else:
            print(f"Failed to fetch user info. Status code: {response.status_code}")
            print(f"Response content: {response.content}")

class WebsiteUser(HttpUser):
    tasks = [UserBehavior]
    wait_time = between(5, 15)
    host = "http://51.8.3.51:5000"  # Update this to the correct API Gateway host
