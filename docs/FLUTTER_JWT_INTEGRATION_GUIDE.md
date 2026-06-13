# JSAP Flutter Authentication Guide

## Authentication Overview

JSAP uses one backend authentication system for both Razor/MVC web users and Flutter mobile users.

Web flow:

```text
Web Login Form
-> POST /api/Auth/login
-> Backend validates username/password
-> Backend creates JWT
-> Backend stores JWT in HttpOnly JSAP.Auth cookie
-> Browser sends cookie automatically
-> ASP.NET JWT bearer middleware reads JSAP.Auth cookie
-> Controllers receive authenticated ClaimsPrincipal
```

Mobile flow:

```text
Flutter Login Screen
-> POST /api/Auth/login with X-Client-Type: Mobile
-> Backend validates username/password
-> Backend creates JWT
-> Backend creates refresh token
-> Backend returns JWT and refresh token in response body
-> Flutter stores both tokens in flutter_secure_storage
-> Flutter sends Authorization: Bearer <jwt>
-> ASP.NET JWT bearer middleware validates bearer token
-> Controllers receive authenticated ClaimsPrincipal
```

Both flows produce the same claims, roles, permissions, company checks, and AdminOnly behavior.

## Login API

URL:

```text
POST /api/Auth/login
```

Headers for Flutter:

```http
Content-Type: application/json
X-Client-Type: Mobile
```

Request:

```json
{
  "loginUser": "kamalpreet",
  "password": "your-password"
}
```

Mobile success response:

```json
{
  "success": true,
  "message": "Login successful",
  "accessToken": "<jwt>",
  "refreshToken": "<refresh-token>",
  "expiresInMinutes": 15,
  "refreshExpiresInDays": 7,
  "user": {
    "userId": 84,
    "userName": "kamalpreet",
    "userEmail": "user@example.com",
    "role": "Admin"
  }
}
```

Web success response does not expose the JWT. The JWT is stored in the `JSAP.Auth` HttpOnly cookie.

Failed response:

```json
{
  "success": false,
  "message": "Invalid username or password"
}
```

## Secure Storage

Use `flutter_secure_storage`. Do not use `shared_preferences`, `localStorage`, or session storage for JWTs.

Example:

```dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class TokenStore {
  static const _storage = FlutterSecureStorage();
  static const _tokenKey = 'jsap_access_token';
  static const _refreshTokenKey = 'jsap_refresh_token';

  Future<void> saveToken(String token) {
    return _storage.write(key: _tokenKey, value: token);
  }

  Future<void> saveRefreshToken(String token) {
    return _storage.write(key: _refreshTokenKey, value: token);
  }

  Future<String?> readToken() {
    return _storage.read(key: _tokenKey);
  }

  Future<String?> readRefreshToken() {
    return _storage.read(key: _refreshTokenKey);
  }

  Future<void> clear() {
    return _storage.deleteAll();
  }
}
```

After login, save both tokens:

```dart
await tokenStore.saveToken(response.data['accessToken']);
await tokenStore.saveRefreshToken(response.data['refreshToken']);
```

## Refresh Token Flow

Access tokens expire after 15 minutes. Refresh tokens expire after 7 days.

Refresh endpoint:

```text
POST /api/Auth/refresh
```

Headers:

```http
Content-Type: application/json
X-Client-Type: Mobile
```

Request:

```json
{
  "refreshToken": "<refresh-token>"
}
```

Success response:

```json
{
  "success": true,
  "message": "Token refreshed",
  "accessToken": "<new-jwt>",
  "refreshToken": "<new-refresh-token>",
  "expiresInMinutes": 15
}
```

The backend rotates refresh tokens. After a successful refresh, the old refresh token is revoked and must be replaced in secure storage.

If a revoked refresh token is reused, the backend returns `401 Unauthorized`.

## Dio Interceptor

Attach the JWT automatically on every API request.

```dart
import 'package:dio/dio.dart';

class AuthInterceptor extends Interceptor {
  AuthInterceptor(this.tokenStore, this.dio);

  final TokenStore tokenStore;
  final Dio dio;
  bool _refreshing = false;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await tokenStore.readToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    options.headers['X-Client-Type'] = 'Mobile';
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        final retryToken = await tokenStore.readToken();
        final retryOptions = err.requestOptions;
        retryOptions.headers['Authorization'] = 'Bearer $retryToken';
        try {
          final response = await dio.fetch(retryOptions);
          return handler.resolve(response);
        } catch (_) {
          await tokenStore.clear();
          // Navigate to Login from app-level auth state/router guard.
        }
      } else {
        await tokenStore.clear();
        // Navigate to Login from app-level auth state/router guard.
      }
    }
    handler.next(err);
  }

  Future<bool> _tryRefresh() async {
    if (_refreshing) return false;
    _refreshing = true;
    try {
      final refreshToken = await tokenStore.readRefreshToken();
      if (refreshToken == null || refreshToken.isEmpty) return false;

      final response = await dio.post(
        '/api/Auth/refresh',
        data: {'refreshToken': refreshToken},
        options: Options(headers: {'X-Client-Type': 'Mobile'}),
      );

      if (response.statusCode == 200 && response.data['success'] == true) {
        await tokenStore.saveToken(response.data['accessToken']);
        await tokenStore.saveRefreshToken(response.data['refreshToken']);
        return true;
      }
      return false;
    } finally {
      _refreshing = false;
    }
  }
}
```

## API Authentication

Authorization header format:

```http
Authorization: Bearer <jwt>
```

Protected API example:

```http
GET /api/Auth/getcompanies
Authorization: Bearer <jwt>
X-Client-Type: Mobile
```

Admin API example:

```http
POST /api/Auth/adminresetpassword
Authorization: Bearer <admin-jwt>
X-Client-Type: Mobile
```

## Token Expiration

Current access token lifetime is 15 minutes.

When any API returns `401 Unauthorized`:

- Try `POST /api/Auth/refresh` once using the stored refresh token.
- If refresh succeeds, store the new access token and new refresh token.
- Retry the failed API request once.
- If refresh fails, clear secure storage, clear app state, and redirect to login.

When any API returns `403 Forbidden`:

- Keep the user logged in.
- Show an access denied message or route back to a permitted screen.

## Logout

Flutter logout should revoke the refresh token, then clear local tokens and app state.

```dart
Future<void> logout(Dio dio, TokenStore tokenStore) async {
  final refreshToken = await tokenStore.readRefreshToken();
  if (refreshToken != null && refreshToken.isNotEmpty) {
    await dio.post(
      '/api/Auth/Logout',
      data: {'token': refreshToken},
      options: Options(headers: {'X-Client-Type': 'Mobile'}),
    );
  }
  await tokenStore.clear();
  // Clear user profile, companies, permissions, and navigation state.
}
```

Mobile logout does not need cookies. The refresh token is revoked server-side. The access token remains valid only until its 15-minute expiry unless invalidated earlier by password, role, lock, disable, or security stamp changes.

## Route Guards

Protect Flutter screens before rendering them.

```dart
Future<bool> isLoggedIn(TokenStore tokenStore) async {
  final token = await tokenStore.readToken();
  return token != null && token.isNotEmpty;
}
```

Guard behavior:

- No token: navigate to login.
- Token exists: allow protected route, then API calls validate it.
- API returns 401: clear token and navigate to login.

## Error Handling

`401 Unauthorized`:

- Missing token
- Invalid token
- Expired token
- Invalid refresh token
- Revoked refresh token replay
- Security stamp invalidated
- User disabled/locked

Action: attempt refresh once when the access token fails. If refresh fails, clear tokens and redirect to login.

`403 Forbidden`:

- Valid user but missing role or policy
- AdminOnly endpoint called by normal user

Action: show access denied.

`500 Internal Server Error`:

- Server-side issue

Action: show retry/error UI and log diagnostics.

## Testing Checklist

- Login with `X-Client-Type: Mobile`.
- Confirm response contains `accessToken`.
- Store token in `flutter_secure_storage`.
- Restart app and read token from secure storage.
- Call `/api/Auth/getcompanies` with `Authorization: Bearer <jwt>`.
- Call a protected API without token and confirm `401`.
- Call a protected API with invalid token and confirm `401`.
- Wait past token expiry and confirm `401`.
- Use refresh token and confirm new `accessToken` and rotated `refreshToken`.
- Reuse old refresh token and confirm `401`.
- Logout revokes refresh token, clears secure storage, and protected screens are blocked.
- Normal user calling AdminOnly endpoint gets `403`.
- Company-restricted API uses authenticated identity and company validation.
