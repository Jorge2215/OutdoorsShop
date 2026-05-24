### 2026-05-24: Enable Swagger in all environments
**By:** Cinnamon (Backend Dev)
**What:** Removed IsDevelopment() guard from Swagger/SwaggerUI setup in Program.cs
**Why:** Backlog item — API docs should always be accessible at /swagger
**Commit:** 9076954d0d896275d691cbf0f75bd8ee216824c0
**Verified:** /swagger returns 200 in production
