# JWT Authentication Guide for HomeBuddy API

## Overview

Your HomeBuddy API uses JWT (JSON Web Token) authentication. This guide explains how to authenticate and use JWT tokens to access protected endpoints.

## Authentication Flow

1. **Register/Login** → Get JWT Token
2. **Include Token** → In Authorization Header
3. **Access Protected Endpoints** → With Valid Token

---

## Step 1: Get a JWT Token

### User Registration
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "YourPassword123!"
}
```

**Response:**
```json
{
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9..."
}
```

### User Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "YourPassword123!"
}
```

**Response:**
```json
{
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9..."
}
```

### Admin Login
```http
POST /api/auth/admin/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "AdminPassword123!"
}
```

**Response:**
```json
{
  "email": "admin",
  "token": "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9..."
}
```

---

## Step 2: Use the JWT Token

Once you have the token, include it in the `Authorization` header of all protected requests.

### Format
```
Authorization: Bearer <your-jwt-token>
```

### Example Request
```http
GET /api/userprofile
Authorization: Bearer eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9...
```

---

## Using in Different Clients

### 1. Swagger UI (Built-in)

1. Start your API
2. Navigate to Swagger UI (usually `https://localhost:7039/swagger`)
3. Click the **"Authorize"** button (lock icon) at the top
4. Enter: `Bearer <your-token>` (include the word "Bearer" and a space)
5. Click **"Authorize"**
6. All protected endpoints will now use this token

### 2. cURL
```bash
curl -X GET "https://localhost:7039/api/userprofile" \
  -H "Authorization: Bearer eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9..."
```

### 3. Postman
1. Create a new request
2. Go to the **Authorization** tab
3. Select **Type: Bearer Token**
4. Paste your token in the **Token** field
5. Send the request

### 4. JavaScript (Fetch API)
```javascript
const token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9...";

fetch('https://localhost:7039/api/userprofile', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

### 5. JavaScript (Axios)
```javascript
const token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9...";

axios.get('https://localhost:7039/api/userprofile', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
.then(response => console.log(response.data));
```

### 6. C# HttpClient
```csharp
var token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9...";
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("https://localhost:7039/api/userprofile");
var content = await response.Content.ReadAsStringAsync();
```

---

## Protected Endpoints

### User Role Required (`[Authorize(Roles = "User")]`)
- `GET /api/userprofile` - Get own profile
- `PUT /api/userprofile` - Update own profile
- `DELETE /api/userprofile` - Delete own account

### Admin Role Required (`[Authorize(Roles = "Admin")]`)
- `GET /api/admins` - Get all admins
- `GET /api/usermanagement` - Get all users
- All `/api/admin/*` endpoints

### User or Admin (`[Authorize(Roles = "Admin,User")]`)
- `GET /api/orders/{id}` - Get specific order
- `POST /api/orders` - Create order
- `GET /api/reviews` - Get reviews (if protected)

---

## Token Details

- **Expiration**: 7 days from issue
- **Algorithm**: HMAC SHA-512
- **Claims**: 
  - `NameIdentifier` (User ID)
  - `Email` (User email)
  - `Role` (User or Admin)

---

## Troubleshooting

### 401 Unauthorized
- **Check**: Token is included in the Authorization header
- **Check**: Token format is correct: `Bearer <token>` (with space)
- **Check**: Token hasn't expired (7 days)
- **Check**: Token was generated with the correct JWT configuration

### 403 Forbidden
- **Check**: User has the required role (User/Admin)
- **Check**: Token contains the correct role claim

### Token Validation Errors
- **Check**: JWT configuration in `appsettings.json` matches
- **Check**: `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` are set correctly

---

## Configuration

JWT settings are in `appsettings.json`:
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "HomeBuddy_API",
    "Audience": "HomeBuddy_API"
  }
}
```

**Important**: In production, use a strong, randomly generated key and store it securely (e.g., Azure Key Vault, environment variables).

---

## Example: Complete Authentication Flow

```javascript
// 1. Login
const loginResponse = await fetch('https://localhost:7039/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'YourPassword123!'
  })
});

const { token } = await loginResponse.json();

// 2. Store token (localStorage, sessionStorage, or secure storage)
localStorage.setItem('jwt_token', token);

// 3. Use token for protected requests
const profileResponse = await fetch('https://localhost:7039/api/userprofile', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const profile = await profileResponse.json();
console.log(profile);
```

---

## Security Best Practices

1. **Never expose tokens** in client-side code that's publicly accessible
2. **Use HTTPS** in production to encrypt token transmission
3. **Store tokens securely** (httpOnly cookies, secure storage)
4. **Implement token refresh** for better security (currently tokens last 7 days)
5. **Validate tokens** on the server side (already implemented)
6. **Use strong JWT keys** in production (minimum 32 characters, randomly generated)
