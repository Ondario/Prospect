# Friend System Implementation

## Overview
This document describes the implementation of the friend system for the Prospect API, including user search functionality and friend relationship management using MongoDB.

## Files Added/Modified

### New Files Created

#### 1. Database Models
- **`Models/FriendModel.cs`** - MongoDB model for friend relationships
  - Stores friend relationships with status (Pending, Accepted, Blocked)
  - Includes timestamps for creation and updates
  - Uses MongoDB ObjectId as primary key

#### 2. Database Services
- **`DbFriendService.cs`** - Service for friend CRUD operations
  - Inherits from `BaseDbService<FriendModel>` following project patterns
  - Methods: GetFriends, GetFriendship, AddFriendRequest, UpdateFriendStatus, GetPendingRequests

#### 3. API Models
- **`Models/Client/FGetAccountInfoRequest.cs`** - Request model for user search
  - Supports search by PlayFabId, TitleDisplayName, Username, or Email
- **`Models/Client/FGetAccountInfoResult.cs`** - Response model for user info
  - Returns user account information including display name, PlayFabId, timestamps

### Modified Files

#### 1. Service Registration
- **`Startup.cs`** - Added `DbFriendService` as singleton
  ```csharp
  services.AddSingleton<DbFriendService>();
  ```

#### 2. Database Service Enhancement
- **`DbUserService.cs`** - Added search methods
  - `FindAsync(string playFabId)` - Find user by PlayFab ID
  - `FindByDisplayNameAsync(string displayName)` - Case-insensitive search by display name
  - `FindByUsernameAsync(string username)` - Search by username (currently maps to display name)
  - `FindByEmailAsync(string email)` - Search by email (placeholder for future implementation)

#### 3. API Controller Enhancement
- **`Controllers/ClientController.cs`** - Added GetAccountInfo endpoint
  - Added `DbFriendService` dependency injection
  - Implemented `GetAccountInfo` endpoint for user search functionality

## API Endpoints

### GetAccountInfo
**Endpoint:** `POST /Client/GetAccountInfo`  
**Authentication:** Required  
**Purpose:** Search for users by various criteria

**Request:**
```json
{
  "PlayFabId": "string",
  "TitleDisplayName": "string", 
  "Username": "string",
  "Email": "string"
}
```

**Response:**
```json
{
  "Code": 200,
  "Status": "OK",
  "Data": {
    "AccountInfo": {
      "PlayFabId": "string",
      "TitleDisplayName": "string",
      "Username": "string",
      "Email": "string",
      "Created": "datetime",
      "LastLogin": "datetime"
    }
  }
}
```

**Search Priority:**
1. PlayFabId (exact match)
2. TitleDisplayName (case-insensitive regex)
3. Username (maps to display name)
4. Email (not implemented yet)

## Database Schema

### FriendModel Collection
```javascript
{
  "_id": ObjectId,
  "userId": "string",           // User who sent the friend request
  "friendUserId": "string",     // User who received the friend request
  "status": "enum",             // Pending, Accepted, Blocked
  "createdAt": "datetime",      // When the relationship was created
  "updatedAt": "datetime"       // When the relationship was last updated
}
```

### FriendStatus Enum
- **Pending** - Friend request sent but not yet accepted/declined
- **Accepted** - Friend request accepted, users are now friends
- **Blocked** - Friend request blocked (can be used for blocking users)

## Service Methods

### DbFriendService
- `GetFriendsAsync(string userId)` - Get all accepted friends for a user
- `GetFriendshipAsync(string userId, string friendUserId)` - Get specific friendship
- `AddFriendRequestAsync(string userId, string friendUserId)` - Send friend request
- `UpdateFriendStatusAsync(string userId, string friendUserId, FriendStatus status)` - Accept/decline/block
- `GetPendingRequestsAsync(string userId)` - Get pending friend requests for a user

### DbUserService (Enhanced)
- `FindAsync(string playFabId)` - Find user by PlayFab ID
- `FindByDisplayNameAsync(string displayName)` - Case-insensitive search
- `FindByUsernameAsync(string username)` - Search by username
- `FindByEmailAsync(string email)` - Search by email (placeholder)

## Integration with Existing Systems

### User System Integration
- Friend system validates that friend requests are only sent to existing users
- User search functionality leverages existing PlayFabUser collection
- Display names and user info are retrieved from the user system

### Authentication Integration
- All friend-related endpoints require authentication
- User context is obtained from JWT tokens
- Proper authorization ensures users can only manage their own friend relationships

## Error Handling

### GetAccountInfo Endpoint
- Returns null AccountInfo when user not found (instead of error)
- Handles exceptions gracefully with 500 status codes
- Logs errors for debugging

### Database Operations
- Uses MongoDB's built-in error handling
- Async operations with proper exception propagation
- Null-safe operations with FirstOrDefaultAsync

## Usage Examples

### Search for User by Display Name
```http
POST /Client/GetAccountInfo
Authorization: Bearer <token>
Content-Type: application/json

{
  "TitleDisplayName": "Liightning"
}
```

### Expected Response for Found User
```json
{
  "Code": 200,
  "Status": "OK",
  "Data": {
    "AccountInfo": {
      "PlayFabId": "12345678-1234-1234-1234-123456789012",
      "TitleDisplayName": "Liightning",
      "Username": "Liightning",
      "Email": null,
      "Created": "2024-01-01T00:00:00Z",
      "LastLogin": "2024-01-15T12:00:00Z"
    }
  }
}
```

### Expected Response for User Not Found
```json
{
  "Code": 200,
  "Status": "OK",
  "Data": {
    "AccountInfo": null
  }
}
```

## Next Steps

### Immediate TODO
1. **Implement Friend Request CloudScript Functions**
   - SendFriendRequest
   - AcceptFriendRequest  
   - DeclineFriendRequest
   - BlockUser

2. **Enhance GetFriendList Function**
   - Return actual friends with user info
   - Include friend status and timestamps

3. **Add Friend Status Tracking**
   - Online/offline status
   - Last seen timestamps

### Future Enhancements
1. **Email Support**
   - Add email field to PlayFabUser model
   - Implement email-based user search

2. **Friend Recommendations**
   - Suggest friends based on mutual connections
   - Activity-based recommendations

3. **Friend Groups**
   - Create friend groups/categories
   - Bulk friend management

## Testing

### Manual Testing
1. Start the API server
2. Use a tool like Postman to test the GetAccountInfo endpoint
3. Search for existing users by display name
4. Verify responses match expected format

### Integration Testing
1. Test with actual game client
2. Verify user search works in friend UI
3. Test error scenarios (invalid tokens, non-existent users)

## Dependencies

### Required Packages
- MongoDB.Driver (already included)
- Microsoft.Extensions.Options (already included)
- System.Text.Json (already included)

### Service Dependencies
- DbUserService (for user validation and info)
- AuthTokenService (for authentication)
- UserDataService (for user data access)

## Notes

- Friend relationships are bidirectional but stored as separate records
- User search is case-insensitive for better user experience
- Email search is placeholder for future implementation
- All timestamps are in UTC
- MongoDB indexes should be added for userId and friendUserId fields for performance

## PlayFabUser Model Updates

### New Properties
- **CreatedAt** (`DateTime`) — Timestamp when the user was created (set automatically)
- **LastLoginAt** (`DateTime?`) — Timestamp of the user's last login (updated on each login)

### Updated Schema Example
```json
{
  "_id": "string", // PlayFabId
  "DisplayName": "string",
  "Auth": [ ... ],
  "CreatedAt": "2024-06-24T12:00:00Z",
  "LastLoginAt": "2024-06-24T12:00:00Z"
}
```

### Usage
- When a user is created, both `CreatedAt` and `LastLoginAt` are set to the current UTC time.
- On subsequent logins, `LastLoginAt` is updated to the current UTC time.
- These fields are used in the `/Client/GetAccountInfo` endpoint to provide account creation and last login information.

### Code Reference
- See `PlayFabUser` in `Models/PlayFabUser.cs`
- See user creation and login logic in `DbUserService.cs` (methods: `CreateAsync`, `FindOrCreateAsync`) 