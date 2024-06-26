from locust import HttpUser, task, between

class UserBehavior(HttpUser):
    wait_time = between(1, 5)

    @task
    def get_user(self):
        user_id = 5160  # Use the appropriate user ID for testing
        self.client.get(f"/api/users/{user_id}")

class WebsiteUser(HttpUser):
    tasks = [UserBehavior]
    wait_time = between(5, 15)
